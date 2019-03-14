using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LyricRobotCommon;
using MachineLearningCommon;
using Microsoft.Data.DataView;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Text;

namespace LyricClassifier
{
    public class Program
    {            

        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public async static Task MainAsync(string[] args)
        {
            Console.WriteLine($"Loading Full Dataset from db");
            DocumentDBRepository<SongRecord>.Initialize(collectionId: "Songs");
            var songs = await DocumentDBRepository<SongRecord>.GetItemsAsync(x => true, -1);

            foreach (var genre in (Genre[])Enum.GetValues(typeof(Genre)))
            {
                Console.WriteLine();
                Console.WriteLine($"=============== Genre: {genre.ToString()}  ===============");

                var lyricData = songs
                                .Select(x =>
                                new Lyric
                                {
                                    Genre = x.Genre.Contains(genre.ToString()),
                                    Text = x.Lyrics
                                });

                // Only bother with data with more than 20 examples
                if (lyricData.Count(x => x.Genre) > 20)
                {
                    Console.WriteLine($"Loading data");
                    var mlContext = new MLContext(seed: 0);

                    IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(lyricData);

                    TrainCatalogBase.TrainTestData splitDataView = mlContext.BinaryClassification.TrainTestSplit(trainingDataView, testFraction: 0.2);                    

                    var model = BuildAndTrainModel(splitDataView.TrainSet,  mlContext);

                    Evaluate(mlContext, model, splitDataView.TestSet);

                    using (var stream = new MemoryStream())
                    {
                        mlContext.Model.Save(model, stream);
                        await BlobRepository<object>.UploadFromStream(stream, $"{genre.ToString()}GenrePrediction");
                    }
                }
                else
                {
                    Console.WriteLine($"Not enough data");
                }
            }
        }


        public static ITransformer BuildAndTrainModel(
                        IDataView trainingDataView, 
                        MLContext mlContext)
        {
            var pipeline = mlContext.Transforms.Text.FeaturizeText(outputColumnName: DefaultColumnNames.Features, inputColumnName: nameof(Lyric.Text))
                .Append(mlContext.BinaryClassification.Trainers.FastTree(numLeaves: 200, numTrees: 200, minDatapointsInLeaves: 20));

            // Train the model fitting to the DataSet
            Console.WriteLine($"Training...");
            var model = pipeline.Fit(trainingDataView);


            Console.WriteLine($"Finished Training the model Ending time: {DateTime.Now.ToString()}");

            return model;

        }

        public static void Evaluate(MLContext mlContext, ITransformer model, IDataView splitTestSet)
        {
            Console.WriteLine("Evaluating Model accuracy with Test dat");
            IDataView predictions = model.Transform(splitTestSet);

            CalibratedBinaryClassificationMetrics metrics = mlContext.BinaryClassification.Evaluate(predictions, "Label");

            Console.WriteLine();
            Console.WriteLine("Model quality metrics evaluation");
            Console.WriteLine("--------------------------------");
            Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
            Console.WriteLine($"Auc: {metrics.Auc:P2}");
            Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
            Console.WriteLine();
        }

    }
}
