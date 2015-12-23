using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RedmartTote
{
    class Program
    {
        static void Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("{0} - Initializing....", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var tote = new Dimension
            {
                Length = 45,
                Width = 30,
                Height = 35
            };

            Console.WriteLine("{0} - Reading products from file....", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var allProducts = GetProducts(tote)
                .OrderBy(x => x.Id)
                .ToList();

            Console.WriteLine("{0} - Processing PickList....", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var maxVolume = tote.Volume;
            var columnSize = maxVolume + 1;
            var pickList = new bool[allProducts.Count, columnSize];
            CalculatePickList(allProducts, columnSize, pickList);

            Console.WriteLine("{0} - Selecting from PickList....", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var selectedProducts = PickUpProducts(tote, allProducts, pickList);

            Console.WriteLine("{0} - Report.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine("Id: {0}", selectedProducts.Sum(x => x.Id));
            Console.WriteLine("Volume: {0} / {1}", selectedProducts.Sum(x => x.Dimension.Volume), tote.Volume);
            Console.WriteLine("Weight: {0}", selectedProducts.Sum(x => x.Weight));
            Console.WriteLine("Total: {0}", selectedProducts.Sum(x => x.Price));

            stopwatch.Stop();
            Console.WriteLine("Elapsed: {0}", stopwatch.Elapsed);
            Console.Read();
        }

        private static List<Product> PickUpProducts(Dimension tote, List<Product> allProducts, bool[,] pickList)
        {
            var selectedProducts = new List<Product>();
            var remainingSpace = tote.Volume;

            for (var i = allProducts.Count - 1; i >= 0; i--)
            {
                var isKeep = pickList[i, remainingSpace];

                if (!isKeep)
                    continue;

                var currentProduct = allProducts[i];
                selectedProducts.Add(currentProduct);
                remainingSpace -= currentProduct.Dimension.Volume;
            }

            foreach (var selectedProduct in selectedProducts)
            {
                Console.WriteLine("id: {0}, Measure: {1}*{2}*{3}, Price: {4}, Weight: {5}",
                    selectedProduct.Id, selectedProduct.Dimension.Length,
                    selectedProduct.Dimension.Width, selectedProduct.Dimension.Height,
                    selectedProduct.Price, selectedProduct.Weight);
            }
            return selectedProducts;
        }

        private static void CalculatePickList(IList<Product> allProducts,
            int columnSize, bool[,] pickList)
        {
            var valueList = new int[columnSize];
            var minVolumeIdx = allProducts.Min(x => x.Dimension.Volume) - 1;

            for (var idx = 0; idx < allProducts.Count; idx++)
            {
                var product = allProducts[idx];
                var tempValueList = new int[columnSize];

                for (var volIdx = minVolumeIdx; volIdx < columnSize; volIdx++)
                {
                    var chosenVal = 0;
                    var keep = false;

                    var isWithin = product.Dimension.Volume <= volIdx;

                    var valueOfCellAbove = valueList[volIdx];

                    if (isWithin)
                    {
                        var remainingSpace = volIdx - product.Dimension.Volume;
                        var otherPossibleVal = valueList[remainingSpace];

                        var finalPossibleVal = product.Price + otherPossibleVal;

                        if (valueOfCellAbove <= finalPossibleVal)
                        {
                            chosenVal = finalPossibleVal;
                            keep = true;
                        }
                        else
                        {
                            chosenVal = valueOfCellAbove;
                            keep = false;
                        }
                    }
                    else
                    {
                        chosenVal = valueOfCellAbove;
                        keep = false;
                    }

                    pickList[idx, volIdx] = keep;

                    if (chosenVal == 0 && volIdx > 0)
                    {
                        tempValueList[volIdx] = tempValueList[volIdx - 1];
                    }
                    else
                    {
                        tempValueList[volIdx] = chosenVal;
                    }
                }

                for (var i = 0; i < columnSize; i++)
                {
                    valueList[i] = tempValueList[i];
                }
            }
        }

        private static IList<Product> GetProducts(Dimension tote)
        {
            var products = new List<Product>();

            var lines = File.ReadAllLines("products.csv");

            foreach (var line in lines)
            {
                var arr = line.Split(',');
                var product = new Product
                {
                    Id = Convert.ToInt32(arr[0]),
                    Price = Convert.ToInt32(arr[1]),
                    Dimension = new Dimension
                    {
                        Length = Convert.ToInt16(arr[2]),
                        Width = Convert.ToInt16(arr[3]),
                        Height = Convert.ToInt16(arr[4])
                    },
                    Weight = Convert.ToInt32(arr[5])
                };

                var oversize = false;

                if (product.Dimension.Length > tote.Length
                    || product.Dimension.Width > tote.Width
                    || product.Dimension.Height > tote.Height)
                {
                    oversize = true;
                }

                if (product.Dimension.Volume > tote.Volume)
                {
                    oversize = true;
                }

                if (oversize)
                {
                    continue;
                }

                var lighterProduct = products.FirstOrDefault(x => x.Dimension == product.Dimension
                                                          && x.Price == product.Price
                                                          && x.Weight < product.Weight);

                if (lighterProduct != null)
                {
                    continue;
                }

                var heavierProduct = products.FirstOrDefault(x => x.Dimension == product.Dimension
                                                          && x.Price == product.Price
                                                          && x.Weight > product.Weight);

                if (heavierProduct != null)
                {
                    products.Remove(heavierProduct);
                }

                products.Add(product);
            }

            return products;
        }
    }

    class Product
    {
        public int Id { get; set; }
        public Dimension Dimension { get; set; }
        public int Price { get; set; }
        public int Weight { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5}",
                    Id, Price, Dimension.Length, Dimension.Width,
                    Dimension.Height, Weight);
        }
    }

    class Dimension
    {
        public short Length { get; set; }
        public short Width { get; set; }
        public short Height { get; set; }

        public int Volume
        {
            get { return Length * Width * Height; }
        }
    }
}
