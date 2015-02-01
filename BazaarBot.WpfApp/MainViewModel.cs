using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using BazaarBot.Engine;

namespace BazaarBot.WpfApp
{
    class MainViewModel : INotifyPropertyChanged
    {
        const int SEED = 133;
        static Engine.BazaarBot bazaar = new Engine.BazaarBot(new StandardRandomNumberGenerator(0));

        public ICommand AdvanceCommand { get; set; }
        public ICommand BenchmarkCommand { get; set; }
        public PlotModel PricePlot { get; private set; }
        public PlotModel TradesPlot { get; private set; }
        public PlotModel SupplyPlot { get; private set; }
        public PlotModel DemandPlot { get; private set; }
        public PlotModel ProfitPlot { get; private set; }
        public int BenchmarkRounds { get; set; }
        public string[][] Report { get; set; }

        public MainViewModel()
        {
            BenchmarkRounds = 30;
            JSONParser.LoadJsonSettings(bazaar, "settings.json");
            AdvanceCommand = new RelayCommand(() => Advance());
            BenchmarkCommand = new RelayCommand(() => Benchmark());
            Plot();
        }

        private void Plot()
        {
            PricePlot = GetPlot("Prices", bazaar.PriceHistory, bazaar.CommodityClasses);
            DemandPlot = GetPlot("Demand", bazaar.BidHistory, bazaar.CommodityClasses);
            SupplyPlot = GetPlot("Supply", bazaar.AskHistory, bazaar.CommodityClasses);
            TradesPlot = GetPlot("Trades", bazaar.TradeHistory, bazaar.CommodityClasses);
            ProfitPlot = UpdatePlot(ProfitPlot, "Profit", bazaar.ProfitHistory, bazaar.AgentClasses.Keys.ToArray());
            Report = new MarketReport(bazaar).GetData();
            OnPropertyChanged("Report");
            OnPropertyChanged("ProfitPlot");
            OnPropertyChanged("PricePlot");
            OnPropertyChanged("SupplyPlot");
            OnPropertyChanged("DemandPlot");
            OnPropertyChanged("TradesPlot");
        }

        private PlotModel UpdatePlot(PlotModel plot, string title, Dictionary<string, List<float>> dictionary, IList<string> keys)
        {
            if (plot == null)
                return GetPlot(title, dictionary, keys);
            else
            {
                for (int i= 0;i<plot.Series.Count;i++)
                {
                    var series = plot.Series[i] as LineSeries;
                    series.Points.Add(new DataPoint(bazaar.TotalRounds, BazaarBot.Engine.BazaarBot.Average(dictionary[keys[i]])));
                }
                plot.InvalidatePlot(true);
                return plot;
            }
        }

        private static PlotModel GetPlot(string title, Dictionary<string, List<float>> dictionary, IEnumerable<string> keys)
        {
            var plot = new PlotModel { Title = title };
            foreach (var key in keys)
            {
                var series = new LineSeries { Title = key };
                for (int i = 0; i < dictionary[key].Count; i++)
                    series.Points.Add(new DataPoint(i, dictionary[key][i]));
                plot.Series.Add(series);
            }
            return plot;
        }

        private void Simulate(int rounds)
        {
            bazaar.simulate(rounds);
            Plot();
        }

        private void Benchmark()
        {
            Simulate(BenchmarkRounds);
        }

        private void Advance()
        {
            Simulate(1);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
