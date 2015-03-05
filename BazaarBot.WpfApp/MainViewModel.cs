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
    class MainViewModel : ViewModel
    {
        static Engine.BazaarBot bazaar;

        public ICommand AdvanceCommand { get; private set; }
        public ICommand BenchmarkCommand { get; private set; }
        public ICommand RestartCommand { get; private set; }
        public ICommand RestartBenchmarkCommand { get; private set; }
        public ICommand ChangeCommand { get; private set; }
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
            BenchmarkRounds = 100;
            Seed = 1;
            Restart();
            AdvanceCommand = new RelayCommand(() => Advance());
            BenchmarkCommand = new RelayCommand(() => Benchmark());
            RestartCommand = new RelayCommand(() => Restart());
            RestartBenchmarkCommand = new RelayCommand(() => RestartAndBenchmark());
            ChangeCommand = new RelayCommand(() => OpenChangeDialog());
        }

        private void OpenChangeDialog()
        {
            var vm = new ChangeViewModel(bazaar);
            var dlg = new ChangeWindow { DataContext = vm };
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                var amount = vm.Amount;
                foreach (var agent in bazaar.Agents.Where(p => p.ClassId == vm.SelectedAgentClass))
                {
                    if (vm.Amount == 0)
                        break;

                    var has = agent.QueryInventory(vm.SelectedCommodity);
                    if (has > 0)
                    {
                        if (has < vm.Amount)
                        {
                            vm.Amount -= (int)has;
                            agent.ChangeInventory(vm.SelectedCommodity, -has);
                        }
                        else
                        {
                            agent.ChangeInventory(vm.SelectedCommodity, -vm.Amount);
                            vm.Amount = 0;
                        }
                    }
                }
                if (vm.Amount != amount)
                {
                    var delta = amount - vm.Amount;
                    var cost = delta * bazaar.GetPriceAverage(vm.SelectedCommodity, 1);
                    var average = cost / bazaar.Agents.Count(p => p.ClassId == vm.SelectedAgentClass);
                    var profitHistory = bazaar.ProfitHistory[vm.SelectedAgentClass];
                    var lastProfit = profitHistory.Last();
                    profitHistory.RemoveAt(profitHistory.Count - 1);
                    profitHistory.Add(lastProfit + average);
                    Plot();
                }
            }
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
            SupplyPlot = GetPlot("Supply / Demand Ratio", bazaar.VarHistory, bazaar.CommodityClasses);
            TradesPlot = GetPlot("Trades", bazaar.TradeHistory, bazaar.CommodityClasses);
            ProfitPlot = UpdatePlot(ProfitPlot, "Profit", bazaar.ProfitHistory, bazaar.AgentClasses.Keys.ToArray());
            Report = new MarketReport(bazaar).GetData();
            OnPropertyChanged("Report");
            OnPropertyChanged("ProfitPlot");
            OnPropertyChanged("PricePlot");
            OnPropertyChanged("SupplyPlot");
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
    }
}
