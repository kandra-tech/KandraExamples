using Kandra.Forms.ApiClients;
using Kandra.Forms.UiBehaviors;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Documents;
using HomeUtilities.Forms.Handlers;

namespace HomeUtilities.Tests.UiBehaviors;

public sealed class UiBehaviorTests
{
    [Fact]
    public async Task InvoiceUiBehavior_RecalculatesLinesAndTotals_WithoutServer()
    {
        var fakeCalcClient = new FakeHandlerClient<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>(req =>
            new InvoiceCalculateResponseDto
            {
                TotalAmount = 420m,
                PaymentDescription = "Calculated description",
                Lines =
                [
                    new InvoiceCalculateLineDto
                    {
                        ServiceId = Guid.NewGuid(),
                        Zone = TariffZone.Single,
                        Quantity = 100m,
                        Price = 4.2m,
                        Amount = 420m,
                    }
                ]
            });

        var record = new InvoiceDto
        {
            ProviderId = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(),
            Date = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            Month = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var behavior = new InvoiceUiBehavior(fakeCalcClient);
        ((IUiBehaviorRecordTarget<InvoiceDto>)behavior).AttachRecord(record);

        await behavior.OnCalculateClicked(CancellationToken.None);
        Assert.Single(record.Lines);
        Assert.Equal(420m, record.TotalAmount);
        Assert.Equal("Calculated description", record.PaymentDescription);

        var line = record.Lines.First();
        line.Quantity = 50m;
        behavior.OnLineQuantityOrPriceChanged(line);
        Assert.Equal(210m, line.Amount);
        Assert.Equal(210m, record.TotalAmount);

        record.Lines.Remove(line);
        behavior.OnLineRemoved(line);
        Assert.Equal(0m, record.TotalAmount);
    }

    [Fact]
    public async Task MeterReadingUiBehavior_FillsLinesFromHandler_AndSkipsWhenPropertyEmpty()
    {
        var meterId = Guid.NewGuid();
        var callCount = 0;
        var fakeFillClient = new FakeHandlerClient<MeterReadingFillRequestDto, MeterReadingFillResponseDto>(_ =>
        {
            callCount++;
            return new MeterReadingFillResponseDto
            {
                Lines =
                [
                    new MeterReadingFillLineDto
                    {
                        MeterId = meterId,
                        Zone = TariffZone.Day,
                        PreviousReading = 123.45m,
                    }
                ]
            };
        });

        var record = new MeterReadingDto
        {
            PropertyId = Guid.Empty,
            Date = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
        };

        var behavior = new MeterReadingUiBehavior(fakeFillClient);
        ((IUiBehaviorRecordTarget<MeterReadingDto>)behavior).AttachRecord(record);

        // Empty PropertyId guard: does not call handler
        await behavior.OnPropertyOrFillChanged(CancellationToken.None);
        Assert.Equal(0, callCount);
        Assert.Empty(record.Lines);

        // Populated PropertyId: calls handler and initializes Reading = PreviousReading
        record.PropertyId = Guid.NewGuid();
        await behavior.OnFillClicked(CancellationToken.None);
        Assert.Equal(1, callCount);
        var line = Assert.Single(record.Lines);
        Assert.Equal(meterId, line.MeterId);
        Assert.Equal(TariffZone.Day, line.Zone);
        Assert.Equal(123.45m, line.PreviousReading);
        Assert.Equal(123.45m, line.Reading);
    }

    private sealed class FakeHandlerClient<TIn, TOut>(Func<TIn, TOut> handler) : IHandlerApiClient<TIn, TOut>
        where TIn : class
        where TOut : class
    {
        public Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken = default) =>
            Task.FromResult(handler(request));
    }
}
