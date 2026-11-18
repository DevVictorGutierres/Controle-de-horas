using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

using ControleTempo.Aplicacao;
using ControleTempo.Dominio;

namespace ControleTempo.Infraestrutura;

sealed class ExportadorExcel : IExportadorRegistros
{
    static readonly XNamespace Excel = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    const string Office = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    const string Package = "http://schemas.openxmlformats.org/package/2006/relationships";
    const string Content = "application/vnd.openxmlformats-officedocument.spreadsheetml";

    public void Exportar(string destino, IReadOnlyList<Registro> registros)
    {
        if (registros.Count > 1_048_575)
            throw new ArgumentException("A quantidade de registros excede o limite de linhas do Excel.");

        string temporario = Path.Combine(Path.GetDirectoryName(destino)!, $".{Guid.NewGuid():N}.tmp");
        try
        {
            using (var zip = ZipFile.Open(temporario, ZipArchiveMode.Create))
            {
                Parte(zip, "[Content_Types].xml", $"""
                    <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                      <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                      <Default Extension="xml" ContentType="application/xml"/>
                      <Override PartName="/xl/workbook.xml" ContentType="{Content}.sheet.main+xml"/>
                      <Override PartName="/xl/styles.xml" ContentType="{Content}.styles+xml"/>
                      <Override PartName="/xl/worksheets/sheet1.xml" ContentType="{Content}.worksheet+xml"/>
                    </Types>
                    """);
                Parte(zip, "_rels/.rels", $"""
                    <Relationships xmlns="{Package}">
                      <Relationship Id="rId1" Type="{Office}/officeDocument" Target="xl/workbook.xml"/>
                    </Relationships>
                    """);
                Parte(zip, "xl/workbook.xml", $"""
                    <workbook xmlns="{Excel}" xmlns:r="{Office}">
                      <bookViews><workbookView/></bookViews><sheets><sheet name="Desenvolvimento" sheetId="1" r:id="rId1"/></sheets>
                    </workbook>
                    """);
                Parte(zip, "xl/_rels/workbook.xml.rels", $"""
                    <Relationships xmlns="{Package}">
                      <Relationship Id="rId1" Type="{Office}/worksheet" Target="worksheets/sheet1.xml"/>
                      <Relationship Id="rId2" Type="{Office}/styles" Target="styles.xml"/>
                    </Relationships>
                    """);
                Parte(zip, "xl/styles.xml", $"""
                    <styleSheet xmlns="{Excel}">
                      <numFmts count="1"><numFmt numFmtId="164" formatCode="[h]:mm:ss"/></numFmts>
                      <fonts count="2">
                        <font><sz val="11"/><name val="Calibri"/></font>
                        <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
                      </fonts>
                      <fills count="3">
                        <fill><patternFill patternType="none"/></fill>
                        <fill><patternFill patternType="gray125"/></fill>
                        <fill><patternFill patternType="solid"><fgColor rgb="FF272727"/><bgColor indexed="64"/></patternFill></fill>
                      </fills>
                      <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
                      <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                      <cellXfs count="4">
                        <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
                        <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/>
                        <xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
                        <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment wrapText="1" vertical="top"/></xf>
                      </cellXfs>
                      <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
                    </styleSheet>
                    """);
                Parte(zip, "xl/worksheets/sheet1.xml", Folha(registros).ToString(SaveOptions.DisableFormatting));
            }

            File.Move(temporario, destino, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporario))
                File.Delete(temporario);
        }
    }

    static XElement Folha(IReadOnlyList<Registro> registros)
    {
        var linhas = new XElement(Excel + "sheetData", new XElement(Excel + "row", new XAttribute("r", 1),
            Celula("A1", "Cliente", 1), Celula("B1", "Atividade", 1),
            Celula("C1", "Descrição", 1), Celula("D1", "Tempo de desenvolvimento", 1)));
        for (int i = 0; i < registros.Count; i++)
        {
            var registro = registros[i];
            int n = i + 2;
            linhas.Add(new XElement(Excel + "row", new XAttribute("r", n),
                Celula($"A{n}", registro.Cliente, 3), Celula($"B{n}", registro.Atividade, 3),
                Celula($"C{n}", registro.Descricao, 3),
                new XElement(Excel + "c", new XAttribute("r", $"D{n}"), new XAttribute("s", 2),
                    new XElement(Excel + "v", registro.Tempo.TotalDays.ToString("R", CultureInfo.InvariantCulture)))));
        }

        return new XElement(Excel + "worksheet",
            new XElement(Excel + "sheetViews", new XElement(Excel + "sheetView", new XAttribute("workbookViewId", 0),
                new XElement(Excel + "pane", new XAttribute("ySplit", 1), new XAttribute("topLeftCell", "A2"),
                    new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(Excel + "cols", Coluna(1, 26), Coluna(2, 33), Coluna(3, 65), Coluna(4, 29)),
            linhas, new XElement(Excel + "autoFilter", new XAttribute("ref", $"A1:D{registros.Count + 1}")));
    }

    static XElement Coluna(int indice, int largura) =>
        new(Excel + "col", new XAttribute("min", indice), new XAttribute("max", indice),
            new XAttribute("width", largura), new XAttribute("customWidth", 1));

    static XElement Celula(string endereco, string texto, int estilo) =>
        new(Excel + "c", new XAttribute("r", endereco), new XAttribute("s", estilo),
            new XAttribute("t", "inlineStr"), new XElement(Excel + "is",
                new XElement(Excel + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), texto)));

    static void Parte(ZipArchive zip, string nome, string xml)
    {
        using var writer = new StreamWriter(zip.CreateEntry(nome).Open(), new UTF8Encoding(false));
        writer.Write(xml);
    }
}
