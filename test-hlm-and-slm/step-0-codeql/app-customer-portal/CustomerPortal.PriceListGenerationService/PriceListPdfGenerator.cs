using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CustomerPortal.PriceListGenerationService;

public static class PriceListPdfGenerator
{
    static PriceListPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static MemoryStream GeneratePdf(int customerNo, string salesOrg, DateOnly priceDate)
    {
        var memoryStream = new MemoryStream();

        Document
            .Create(document =>
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Text("Products Price List").FontSize(18).Bold();
                            column.Item().Text($"Price date: {priceDate.ToString("O")}");
                        });

                    page.Content()
                        .PaddingVertical(0.5f, Unit.Centimetre)
                        .Column(column =>
                        {
                            column
                                .Item()
                                .Height(5, Unit.Centimetre)
                                .Row(row =>
                                {
                                    row.RelativeItem(0.7f)
                                        .Column(innerColumn =>
                                        {
                                            innerColumn.Item().Text($"Customer No: {customerNo}");
                                            innerColumn.Item().Text($"Name: {Placeholders.Name()}");
                                        });

                                    row.RelativeItem(0.3f).Image(Placeholders.Image);
                                });

                            column
                                .Item()
                                .Height(3, Unit.Centimetre)
                                .Row(row =>
                                {
                                    row.RelativeItem(0.5f);
                                    row.RelativeItem(0.5f)
                                        .Column(innerColumn =>
                                        {
                                            innerColumn.Item().Text($"Company Inc.");
                                            innerColumn.Item().Text($"Sales Org: {salesOrg}");
                                        });
                                });

                            column.Item().Height(2, Unit.Centimetre);

                            column
                                .Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(50);
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(125);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().BorderBottom(2).Padding(8).Text("#");
                                        header.Cell().BorderBottom(2).Padding(8).Text("Product");
                                        header
                                            .Cell()
                                            .BorderBottom(2)
                                            .Padding(8)
                                            .AlignRight()
                                            .Text("Price");
                                    });

                                    foreach (var i in Enumerable.Range(0, 6))
                                    {
                                        var price = Math.Round(Random.Shared.NextDouble() * 100, 2);

                                        table.Cell().Padding(8).Text($"{i + 1}");
                                        table.Cell().Padding(8).Text(Placeholders.Label());
                                        table.Cell().Padding(8).AlignRight().Text($"${price}");
                                    }
                                });
                        });

                    page.Footer().Text("Company Inc.").FontSize(12).Bold();
                })
            )
            .GeneratePdf(memoryStream);

        memoryStream.Position = 0;
        return memoryStream;
    }
}
