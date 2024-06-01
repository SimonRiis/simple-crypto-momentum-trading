using MathNet.Numerics.LinearRegression;
using MathNet.Numerics.Statistics;
using Skender.Stock.Indicators;

namespace core
{
    public static class DecimalExtensions
    {
        public static bool IsNaN(this decimal value)
        {
            return decimal.MinValue == value;
        }
    }
    public class QuoteHelper
    {
        public decimal CalculateAnnualizedSlope(Quote[] timeSeriesData)
        {
            // Sort the data by date to ensure chronological order
            var orderedData = timeSeriesData.OrderBy(data => data.Date).ToArray();

            // Extract the values from the sorted data
            decimal[] ts = orderedData.Select(data => data.Close).ToArray();

            // Remove NaN values from the time series
            ts = ts.Where(x => !DecimalExtensions.IsNaN(x)).ToArray();
            if (ts.Length == 0)
            {
                throw new ArgumentException("Time series contains only NaN values");
            }

            // Generate x values (indices)
            double[] x = Enumerable.Range(0, ts.Length).Select(i => (double)i).ToArray();

            // Calculate log of the time series using double conversion
            double[] logTs = ts.Select(v => Math.Log((double)v)).ToArray();

            // Perform linear regression
            var ols = SimpleRegression.Fit(x, logTs);

            decimal slope = (decimal)ols.Item2;
            decimal intercept = (decimal)ols.Item1;

            // Calculate r_value (correlation coefficient)
            double r_value = Correlation.Pearson(x, logTs);

            // Calculate annualized slope
            decimal annualizedSlope = (decimal)(Math.Pow(Math.Exp((double)slope), 365) - 1) * 100;

            // Adjust by r-squared
            return annualizedSlope * (decimal)Math.Pow(r_value, 2);
        }
    }
}
