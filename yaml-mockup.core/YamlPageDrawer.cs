using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Diagnostics;

namespace YamlMockup.Core;

//Microcharts.LineChart;
//Microcharts.PieChart;
//Microcharts.BarChart;
//Microcharts.DonutChart;
//Microcharts.RadarChart;
//Microcharts.PointChart;
//Microcharts.BarChart;

public class YamlPageDrawer
{
    private const int Step = 50;

    private const ushort FooterSize = 200;

    private const ushort DefaultMinY = 1080;
    private const ushort DefaultMinX = 1920;

    private const ushort DefaultFontSize = 20;
    private const string Font = "3270Medium NF";

    public YamlPageDrawer()
    {
        FontManager.RegisterFont(File.OpenRead("3270-Medium Nerd Font Complete Mono Windows Compatible.ttf"));
    }

    public byte[] DrawPdf(PageContent pageContent)
    {
        return Draw(pageContent).GeneratePdf();
    }

    public byte[] DrawPdf(IEnumerable<PageContent> pageContents)
    {
        Document document = Document.Create(container =>
        {
            foreach (PageContent pageContent in pageContents)
            {
                DrawPage(container, pageContent);
            }
        });

        return document.GeneratePdf();
    }

    public byte[] DrawImage(PageContent pageContent)
    {
        return Draw(pageContent).GenerateImages().First();
    }

    private Document Draw(PageContent pageContent)
    {
        Document document = Document.Create(container => DrawPage(container, pageContent));

        return document;
    }

    public IDocumentContainer DrawPage(IDocumentContainer document, PageContent pageContent)
    {
        for (int i = 0; i < pageContent.ItemContents.Length; i++)
        {
            ItemContent item = pageContent.ItemContents[i];

            if (item.Y.IsBContent())
            {
                string[] yStrings = item.Y.SecondObject.Split(':');
                short y = short.Parse(yStrings[0].Trim());
                short height = short.Parse(yStrings[1].Trim());
                item.Y = new(y);
                item.Height = height;
            }

            if (item.X.IsBContent())
            {
                string[] xStrings = item.X.SecondObject.Split(':');
                short x = short.Parse(xStrings[0].Trim());
                short width = short.Parse(xStrings[1].Trim());
                item.X = new(x);
                item.Width = width;
            }
        }

        int maxX = pageContent.ItemContents.Max(c => Math.Abs(c.X.FirstObject) + c.Width) + FooterSize;
        int maxY = pageContent.ItemContents.Max(c => Math.Abs(c.Y.FirstObject) + c.Height) + FooterSize;

        if ((pageContent.MinX ?? DefaultMinX) > maxX)
        {
            maxX = pageContent.MinX ?? DefaultMinX;
        }
        if ((pageContent.MinY ?? DefaultMinY) > maxY)
        {
            maxY = pageContent.MinY ?? DefaultMinY;
        }

        document = document.Page(page =>
        {
            SetStyle(pageContent.PageName, maxX, maxY, page);
            SetFooter(pageContent, page);

            page.Content().Border(1).Layers(layers =>
            {
                List<Action<ColumnDescriptor>> textActions = new();

                Dictionary<string, IEnumerable<ItemContent>> groupedContents = pageContent.ItemContents
                    .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                    .GroupBy(c => c.Name!)
                    .ToDictionary(d => d.Key, d => d.AsEnumerable());

                foreach (ItemContent itemContent in pageContent.ItemContents)
                {
                    if (itemContent.Text is not null && itemContent.Type is not ItemContentType.Table)
                    {
                        textActions.Add(c => c
                            .Item()
                            .Unconstrained()
                            .TranslateY(itemContent.Y.FirstObject + itemContent.Height / itemContent.Text.Length)
                            .TranslateX(itemContent.X.FirstObject + itemContent.Width / itemContent.Text.Length)
                            .Border((itemContent.Width > 0 && itemContent.Height > 0) ? 0f : itemContent.Border ?? 0f)
                            .Text(itemContent.Text)
                            .FontSize(itemContent.FontSize ?? DefaultFontSize)
                        );
                    }

                    layers.Layer().Element(c => DrawItem(c, itemContent, groupedContents.Keys));
                };

                layers.PrimaryLayer().Column(c => textActions.ForEach(a => a(c)));

                foreach (ItemContent itemContent in pageContent.ItemContents)
                {
                    if (!string.IsNullOrWhiteSpace(itemContent.Link) && !itemContent.HideLink && groupedContents.TryGetValue(itemContent.Link, out IEnumerable<ItemContent>? linkedContents))
                    {
                        DrawLink(layers, itemContent, linkedContents);
                    }
                }

                if (pageContent.Debug)
                {
                    layers.Layer().Element(c => DrawGrid(c, maxX, maxY));
                }
            });

        });

        return document;
    }

    private static void DrawGrid(IContainer container, int maxX, int maxY)
    {
        byte[] data;

        maxX = (int)((maxX - FooterSize / 2) * 0.99);
        maxY = (int)((maxY - FooterSize));

        using (SKSurface sKSurface = SKSurface.Create(new SKImageInfo(maxX, maxY)))
        {
            int xStep = maxX / Step;
            int yStep = maxY / Step;

            using SKPaint paint = new()
            {
                Color = SKColors.NavajoWhite,
                StrokeWidth = 1,
            };

            using SKPaint textPaint = new()
            {
                Color = SKColors.DarkGray,
                StrokeWidth = 1,
            };

            foreach (int xItem in Enumerable.Range(0, xStep + 2))
            {
                sKSurface.Canvas.DrawText((xItem * Step).ToString(), new(xItem * Step, 12), textPaint);
                sKSurface.Canvas.DrawLine(xItem * Step, 0, xItem * Step, maxY, paint);
            }

            foreach (int yItem in Enumerable.Range(0, yStep + 1))
            {
                sKSurface.Canvas.DrawText((yItem * Step).ToString(), new(2, yItem * Step), textPaint);
                sKSurface.Canvas.DrawLine(0, yItem * Step, maxX, yItem * Step, paint);
            }

            sKSurface.Canvas.Save();

            data = sKSurface.Snapshot().Encode(SKEncodedImageFormat.Png, 90).ToArray();
        }

        container
            .Width(maxX)
            .Height(maxY)
            .TranslateX(0)
            .TranslateY(0)
            .Image(data);
    }

    private static void DrawLink(LayersDescriptor layers, ItemContent itemContent, IEnumerable<ItemContent> linkedContents)
    {
        foreach (ItemContent linkedContent in linkedContents)
        {
            layers.Layer().Element(c =>
            {
                byte[] data;

                int width = linkedContent.X.FirstObject - itemContent.X.FirstObject;
                int height = linkedContent.Y.FirstObject - itemContent.Y.FirstObject;

                if (width == 0)
                {
                    width = 2;
                }

                if (height == 0)
                {
                    height = 2;
                }

                using (SKSurface sKSurface = SKSurface.Create(new SKImageInfo(Math.Abs(width), Math.Abs(height))))
                {
                    using SKPaint paint = new()
                    {
                        Color = SKColors.DarkGray,
                        StrokeWidth = 2,
                    };

                    int startXDraw = width < 0 ? -width : 0;
                    int startYDraw = height < 0 ? -height : 0;

                    int endXDraw = width > 0 ? width : 0;
                    int endYDraw = height > 0 ? height : 0;

                    sKSurface.Canvas.DrawLine(
                        startXDraw,
                        startYDraw,
                        endXDraw,
                        endYDraw,
                        paint
                    );

                    sKSurface.Canvas.Save();

                    data = sKSurface.Snapshot().Encode(SKEncodedImageFormat.Png, 90).ToArray();
                }

                short startX = Math.Min(linkedContent.X.FirstObject, itemContent.X.FirstObject);
                short startY = Math.Min(linkedContent.Y.FirstObject, itemContent.Y.FirstObject);

                c.Width(Math.Abs(width))
                .Height(Math.Abs(height))
                .TranslateX(startX)
                .TranslateY(startY)
                .Image(data);
            });
        }
    }

    private static void DrawItem(IContainer layer, ItemContent itemContent, IEnumerable<string> allNames)
    {
        IContainer container = BaseDraw(layer, itemContent, allNames);
        
        if (itemContent.Type is ItemContentType.Rectangle)
        {
            DrawRectangle(itemContent, container);
            return;
        }

        if (itemContent.Type is ItemContentType.Note)
        {
            DrawNote(itemContent, container);
            return;
        }

        if (itemContent.Type is ItemContentType.Table && itemContent.Content is not null)
        {
            DrawTable(itemContent, container);
            return;
        }
    }

    private static void DrawTable(ItemContent itemContent, IContainer container)
    {
        container
            .Table(c =>
            {
                c.ColumnsDefinition(d =>
                {
                    foreach (string item in itemContent.Content!.Split('|'))
                    {
                        d.RelativeColumn();
                    }
                });

                c.Header(h =>
                {
                    foreach (string item in itemContent.Content!.Split('|'))
                    {
                        h.Cell().Element(c =>
                        {
                            c = c.PaddingLeft(3).PaddingVertical(5).BorderBottom(1);
                            return itemContent.AllignLeft ? c.AlignLeft() : c.AlignCenter();
                        }).Text(item.Trim()).Bold();
                    }
                });

                if (itemContent.Text is not null)
                {
                    foreach (string item in itemContent.Text.Split('|'))
                    {
                        c.Cell().Element(c => (itemContent.AllignLeft ? c.AlignLeft() : c.AlignCenter())
                            .PaddingLeft(3)
                            .PaddingVertical(5))
                            .Text(item.Trim());
                    }
                }
            });
    }

    private static void DrawRectangle(ItemContent itemContent, IContainer container)
    {
        if (!string.IsNullOrWhiteSpace(itemContent.Content))
        {
            try
            {
                container.Image($"images/baseline_{itemContent.Content}_black_48dp.png", ImageScaling.FitArea);
                return;
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Error("Ex while load image from {content} {ex}", itemContent.Content, ex);
            }
        }
        
        if (itemContent.Width > 0 && itemContent.Height > 0)
        {
            byte[] data;

            using (SKSurface sKSurface = SKSurface.Create(new SKImageInfo(itemContent.Width, itemContent.Height)))
            {
                using SKPaint paint = new()
                {
                    Color = SKColors.White
                };

                sKSurface.Canvas.DrawRect(0f, 0f, itemContent.Width, itemContent.Height, paint);

                sKSurface.Canvas.Save();

                data = sKSurface.Snapshot().Encode(SKEncodedImageFormat.Jpeg, 90).ToArray();
            }

            container.Image(data);
        }
    }

    private static IContainer BaseDraw(IContainer container, ItemContent itemContent, IEnumerable<string> allNames)
    {
        IContainer drawedContainer = container
            .Section(itemContent.Name ?? string.Empty)
            .TranslateX(itemContent.X.FirstObject)
            .TranslateY(itemContent.Y.FirstObject)
            .Height(itemContent.Height)
            .Width(itemContent.Width);

        if (!string.IsNullOrWhiteSpace(itemContent.Link) && !allNames.Contains(itemContent.Link))
        {
            drawedContainer = drawedContainer.Hyperlink(itemContent.Link);
        }

        return drawedContainer
            .SectionLink(itemContent.Link ?? string.Empty)
            .Border(itemContent.Border ?? 0);
    }

    private static void DrawNote(ItemContent itemContent, IContainer container)
    {
        container.Background(Colors.Grey.Lighten3);
    }

    private static void SetFooter(PageContent pageContent, PageDescriptor page)
    {
        page.Footer().AlignCenter().Text(pageContent.PageComment);
    }

    private static void SetStyle(string page, int maxX, int maxY, PageDescriptor pageDescriptor)
    {
        pageDescriptor.Size(new PageSize(maxX, maxY));

        pageDescriptor.Margin(2, Unit.Centimetre);
        pageDescriptor.PageColor(Colors.White);
        pageDescriptor.DefaultTextStyle(x => x
            .FontSize(DefaultFontSize)
            .FontFamily(Font)
        );

        pageDescriptor.Header()
            .AlignCenter()
            .Text(page)
            .SemiBold()
            .FontSize(36)
            .FontColor(Colors.Blue.Medium);
    }
}
