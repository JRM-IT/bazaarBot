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

namespace BazaarBot.WpfApp
{
    class MainViewModel : INotifyPropertyChanged
    {
        const int SEED = 0;

        public ICommand AdvanceCommand { get; set; }
        public ICommand BenchmarkCommand { get; set; }

        static Engine.BazaarBot bazaar = new Engine.BazaarBot(SEED);

        public MainViewModel()
        {
            BenchmarkRounds = 30;
            bazaar.LoadJsonSettings("settings.json");
            AdvanceCommand = new RelayCommand(() => Advance());
            BenchmarkCommand = new RelayCommand(() => Benchmark());
            Plot();
        }

        private void Plot()
        {
            Model = new PlotModel { Title = "bazaarBot", PlotType = PlotType.XY };
            foreach (var commodity in bazaar.CommodityClasses)
            {
                var series = new LineSeries { Title = commodity + " price" };
                
                for (int i = 0; i < bazaar.PriceHistory[commodity].Count; i++)
                    series.Points.Add(new DataPoint(i, bazaar.PriceHistory[commodity][i]));
                
                Model.Series.Add(series);
            }
            OnPropertyChanged("Model");
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

        public PlotModel Model { get; private set; }

        public int BenchmarkRounds { get; set; }

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
