using System;
using Ticketing.WebApi.Models;

namespace Ticketing.WebApi.Helpers
{
    public static class Calculator
    {
        public static decimal ConditionOne(Rates item, DateTime timeIn, DateTime timeOut, ref int remaining)
        {
            var mins = timeOut - timeIn;
            if (mins.TotalMinutes <= item.Minutes)
            {
                remaining = 0;
                return item.Amount;
            }

            if (item.Repeat > 1)
            {
                decimal totalAmount = 0;
                var total = 0;
                for (int i = 1; i <= item.Repeat; i++)
                {
                    var totalMins = item.Minutes * i;
                    if (mins.TotalMinutes <= totalMins)
                    {
                        total += item.Minutes;
                        totalAmount += item.Amount;
                    }
                }
                remaining = (int)mins.TotalMinutes - total;
                return totalAmount;
            }
            else
            {
                remaining = (int)mins.TotalMinutes - item.Minutes;
                return item.Amount;
            }
        }
        public static decimal ConditionTwo(Rates item, int mins)
        {
            decimal additional = 0;
            if (mins <= 0)
                return 0;

            if (mins < item.Minutes)
                return item.Minutes;

            decimal repeated = (decimal)mins / (decimal)item.Minutes;

            var result = Math.Ceiling(repeated);
            additional = result * item.Amount;

            return additional;
        }
    }
}