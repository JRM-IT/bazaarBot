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
        static Engine.BazaarBot bazaar;

        public ICommand AdvanceCommand { get; set; }
        public ICommand BenchmarkCommand { get; set; }
        public ICommand RestartCommand { get; set; }
        public ICommand RestartBenchmarkCommand { get; set; }
        public PlotModel PricePlot { get; private set; }
        public PlotModel TradesPlot { get; private set; }
        public PlotModel SupplyPlot { get; private set; }
        public PlotModel DemandPlot { get; private set; }
        public PlotModel ProfitPlot { get; private set; }
        public int BenchmarkRounds { get; set; }
        public int Seed { get; set; }
        public string[][] Report { get; set; }

        public MainViewModel()
        {
            BenchmarkRounds = 30;
            Restart();
            AdvanceCommand = new RelayCommand(() => Advance());
            BenchmarkCommand = new RelayCommand(() => Benchmark());
            RestartCommand = new RelayCommand(() => Restart());
            RestartBenchmarkCommand = new RelayCommand(() => RestartAndBenchmark());
        }

        private void RestartAndBenchmark()
        {
            Restart();
            Benchmark();
        }

        private void Restart()
        {
            bazaar = new Engine.BazaarBot(new StandardRandomNumberGenerator(Seed));
            JSONParser.LoadJsonSettings(bazaar, "settings.json");
            ProfitPlot = null;
            Plot();
        }

        private void Plot()
        {
            PricePlot = GetPlot("Prices", bazaar.PriceHistory, bazaar.CommodityClasses, 100);
            SupplyPlot = GetPlot("Supply", bazaar.VarHistory, bazaar.CommodityClasses);
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

        private static PlotModel GetPlot(string title, Dictionary<string, List<float>> dictionary, IEnumerable<string> keys, int limit = 9999999)
        {
            var plot = new PlotModel { Title = title };
            foreach (var key in keys)
            {
                var list = dictionary[key];
                var series = new LineSeries { Title = key };
                var skip = dictionary[key].Count - limit;
                if (skip > 0)
                    list = list.Skip(skip).Take(limit).ToList();
                for (int i = 0; i < list.Count; i++)
                    series.Points.Add(new DataPoint(i, list[i]));
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
