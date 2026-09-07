using HighThroughputDataGrid.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;

namespace HighThroughputDataGrid.BusinessLogic
{
    public partial class HighOutputDataSource
    {
        private const int MINIMAL_NUMBER_OF_GENERATORS = 200;
 
        private DateTime _marketOpen;
        private int _numberOfRandomGenerators;

        public HighOutputDataSource(int numberOfRandomGenerators = MINIMAL_NUMBER_OF_GENERATORS)
        {
            NumberOfRandomGenerators = numberOfRandomGenerators;
            _marketOpen = DateTime.Now;            
        }

        public Observable<double> FreshRateObserver { get; set; }

        public int NumberOfRandomGenerators
        {
            get { return _numberOfRandomGenerators; }
            set
            {
                if (_numberOfRandomGenerators != value)
                {
                    _numberOfRandomGenerators = value < MINIMAL_NUMBER_OF_GENERATORS ? 
                        MINIMAL_NUMBER_OF_GENERATORS : value;
                }
            }
        }

        public void Start() { Task.Run(() => Run()); }

        private void Run()
        {
            int loops = 0;
            int numberOfRandomGenerators = NumberOfRandomGenerators;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Random[] randoms = new Random[numberOfRandomGenerators];
            foreach (int i in Enumerable.Range(0, numberOfRandomGenerators))
                randoms[i] = new Random(Guid.NewGuid().GetHashCode());
            CountdownEvent countdown = new CountdownEvent(Stocks.Count);

            while (true)
            {
                TimeSpan sinceOpen = DateTime.Now - _marketOpen;
                double tick = sinceOpen.TotalSeconds / 28800;

                for (int i = 0; i < Stocks.Count; i++)
                {
                    Thread.Sleep(1);
                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        int index = (int)o;
                        Stock stock = (Stock)Stocks[index];
                        stock.Volumn += (int)(tick * stock.LastVolumn);
                        Random random = randoms[(sinceOpen.Milliseconds + index) % numberOfRandomGenerators];
                        double factor = random.NextDouble();
                        if (stock.Volumn > 0)
                        {
                            if (stock.Last < 0.00001)
                                stock.Last = (factor + 9999.5) / 10000.0 * stock.Close;
                            else stock.Last *= (factor + 9999.5) / 10000.0;
                            double high = stock.High;
                            double low = stock.Low;
                            if (high < 0.00001)
                                high = stock.Last;
                            if (low < 0.00001)
                                low = stock.Last;
                            stock.High = Math.Max(stock.Last, high);
                            stock.Low = Math.Min(stock.Last, low);
                            stock.Bid = stock.Last * (factor + 99.0) / 100.0;
                            stock.Ask = stock.Last * (factor + 100.0) / 100.0;
                            stock.Change = stock.Last - stock.Close;
                            stock.ChangeInPercentage = stock.Change / stock.Close;
                        }
                        countdown.Signal();
                    }, i);
                }
                countdown.Wait();
                countdown.Reset();
                loops++;
                if (loops % 500 == 0)
                {
                    stopwatch.Stop();
                    if (FreshRateObserver != null)
                        FreshRateObserver.Notify((double)loops / ((double)stopwatch.ElapsedMilliseconds / 1000.0));
                    loops = 0;
                    stopwatch.Restart();
                }
            }
        }
    }
}
