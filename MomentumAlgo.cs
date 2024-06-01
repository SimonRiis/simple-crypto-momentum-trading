using Skender.Stock.Indicators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class QuoteWithSlope
    {
        public Quote Quote { get; set; }
        public string Symbol { get; set; }
        public decimal Slope { get; set; }
    }

    public class MomentumAlgo(QuoteHelper quoteHelper)
    {
        public IList<QuoteWithSlope> GetMomentumResults(IList<KrakenKlineWithSymbol> klineWithSymbols, int lookback, int lagDays, decimal threshold, DateTime date)
        {
            var dataToCheck = klineWithSymbols.Where(x => x.Quote.Date >= date.AddDays(-lookback) && x.Quote.Date <= date); //get quotes within range

            var groupedData = dataToCheck.GroupBy(k => k.Symbol).ToDictionary(g => g.Key, g => g.Select(k => k.Quote).OrderBy(q => q.Date).ToList());
            var quotesWithSlope = new List<QuoteWithSlope>();

            if (groupedData.ContainsKey("XBT/EUR"))
            {
                var btcPrices = groupedData["XBT/EUR"]; //get BTC prices for reference

                foreach (var pair in groupedData)
                {
                    var quoteClosestToDate = pair.Value.OrderByDescending(x => x.Date).FirstOrDefault(x => x.Date <= date);

                    if (quoteClosestToDate != null)
                    {
                        var dateForQuote = quoteClosestToDate.Date;

                        var btcPricesBeforeAndOnDate = btcPrices.Where(x => x.Date <= dateForQuote).OrderByDescending(x => x.Date);

                        //check if above threshold 

                        var btcPriceNearestDate = btcPricesBeforeAndOnDate.FirstOrDefault();
                        if (btcPriceNearestDate != null)
                        {
                            var btcPriceAtLagDate = btcPricesBeforeAndOnDate.FirstOrDefault(x => x.Date <= btcPriceNearestDate.Date.AddDays(-lagDays));

                            if (btcPriceAtLagDate != null)
                            {
                                var percentageChange = (btcPriceNearestDate.Close / btcPriceAtLagDate.Close - 1);

                                if (percentageChange > threshold)
                                {
                                    //btc price change is positive for period. we can trade
                                    var quotesToCheck = groupedData[pair.Key].Where(x => x.Date <= dateForQuote).ToArray();
                                    var slope = quoteHelper.CalculateAnnualizedSlope(quotesToCheck);

                                    var quoteWithSlope = new QuoteWithSlope { Quote = quoteClosestToDate, Slope = slope, Symbol = pair.Key };
                                    quotesWithSlope.Add(quoteWithSlope);
                                }
                                else
                                {
                                    //no trade..
                                }

                            }
                        }
                    }

                }
            }

            //get five largest
            var largest = quotesWithSlope.OrderByDescending(x => x.Slope).Take(5);
            return largest.ToList();
        }

    }

}
