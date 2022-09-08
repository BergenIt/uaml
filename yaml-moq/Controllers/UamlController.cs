using Microsoft.AspNetCore.Mvc;
using PdfToSvg;
using YamlMockup.Core;

namespace YamlMockup.Controllers;

/// <summary>
/// Api генерации pdf из Uaml файлов
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UamlController : ControllerBase
{
    private const string NameBase = "Uaml";
    private const string ContentType = "application/pdf";

    private readonly YamlPageDrawer _yamlPageDrawer = new();

    /// <summary>
    /// Сгенерировать Pdf из Uaml файла
    /// </summary>
    /// <param name="yaml">Содержимое Uaml файла</param>
    /// <returns>Pdf с мокапом</returns>
    [HttpPut(nameof(GeneratePage))]
    public FileContentResult GeneratePage([FromBody] string yaml = Default.DefaultValue)
    {
        PageContent pageContent = PageContent.Parse(yaml);

        byte[] data = _yamlPageDrawer.DrawPdf(pageContent);

        return base.File(data, ContentType, GetFileName(pageContent.PageName) + ".pdf");
    }

    /// <summary>
    /// Сгенерировать Png из Uaml файла
    /// </summary>
    /// <param name="yaml">Содержимое Uaml файла</param>
    /// <returns>Png с мокапом</returns>
    [HttpPut(nameof(GeneratePageAsPng))]
    public FileContentResult GeneratePageAsPng([FromBody] string yaml = Default.DefaultValue)
    {
        PageContent pageContent = PageContent.Parse(yaml);

        byte[] data = _yamlPageDrawer.DrawImage(pageContent);

        return base.File(data, "image/png", GetFileName(pageContent.PageName) + ".png");
    }

    /// <summary>
    /// Сгенерировать Svg из Uaml файла
    /// </summary>
    /// <param name="yaml">Содержимое Uaml файла</param>
    /// <returns>Svg с мокапом</returns>
    [HttpPut(nameof(GeneratePageAsSvg))]
    public FileContentResult GeneratePageAsSvg([FromBody] string yaml = Default.DefaultValue)
    {
        PageContent pageContent = PageContent.Parse(yaml);

        Stream stream = _yamlPageDrawer.DrawPdfAsStream(pageContent);

        PdfToSvg.PdfDocument document = PdfToSvg.PdfDocument.Open(stream, false);

        MemoryStream memoryStream = new();

        document.Pages.First().SaveAsSvg(memoryStream);

        byte[] data = memoryStream.ToArray();

        return base.File(data, "application/svg", GetFileName(pageContent.PageName) + ".svg");
    }

    /// <summary>
    /// Сгенерировать pdf из набора Uaml файлов
    /// </summary>
    /// <param name="name">Имя генерируемого проекта</param>
    /// <param name="yamls">Содержимое Uaml файлов</param>
    /// <returns>Pdf с мокапом</returns>
    [HttpPut(nameof(GenerateProject) + "/{name}")]
    public FileContentResult GenerateProject([FromRoute] string name, [FromBody] string[] yamls)
    {
        List<PageContent> pageContents = new(yamls.Length);

        foreach (string yaml in yamls)
        {
            pageContents.Add(PageContent.Parse(yaml));
        }

        byte[] data = _yamlPageDrawer.DrawPdf(pageContents);

        return base.File(data, ContentType, GetFileName(name));
    }

    private static string GetFileName(string name)
    {
        return $"{NameBase}-{name}-{DateTime.Now.ToShortDateString()}-{DateTime.Now.ToShortTimeString()}";
    }
}

