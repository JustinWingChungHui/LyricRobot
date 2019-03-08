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
            Console.WriteLine($"=============== Loading Dataset  ===============");
            DocumentDBRepository<SongRecord>.Initialize(collectionId: "SongsTrainingData");
            var songs = await DocumentDBRepository<SongRecord>.GetItemsAsync(x => true, -1);
            var lyricData = songs
                            .Select(x =>
                            new Lyric
                            {
                                Genre = x.Genre,
                                Text = x.Lyrics
                            });

            var mlContext = new MLContext(seed: 0);

            IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(lyricData);

            Console.WriteLine($"=============== Finished Loading Dataset  ===============");

            var pipeline = ProcessData(mlContext);

            // <SnippetCallBuildAndTrainModel>
            var model = BuildAndTrainModel(trainingDataView, pipeline, mlContext);
            // </SnippetCallBuildAndTrainModel>

            using (var stream = new MemoryStream())
            {
                mlContext.Model.Save(model, stream);
                await BlobRepository<object>.UploadFromStream(stream, "GenrePrediction");
            }
        }

        public static EstimatorChain<ITransformer> ProcessData(MLContext mlContext)
        {
            Console.WriteLine($"=============== Processing Data ===============");
            // STEP 2: Common data process configuration with pipeline data transformations
            // <SnippetMapValueToKey>
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: "Genre", outputColumnName: DefaultColumnNames.Label)
                            // </SnippetMapValueToKey>
                            // <SnippetFeaturizeText>
                            .Append(mlContext.Transforms.Text.FeaturizeText(inputColumnName: "Text", outputColumnName: DefaultColumnNames.Features))
                            // </SnippetFeaturizeText>
                            // <SnippetConcatenate>
                            //.Append(_mlContext.Transforms.Concatenate("Features", "TitleFeaturized", "DescriptionFeaturized"))
                            // </SnippetConcatenate>
                            //Sample Caching the DataView so estimators iterating over the data multiple times, instead of always reading from file, using the cache might get better performance.
                            // <SnippetAppendCache>
                            .AppendCacheCheckpoint(mlContext);
            // </SnippetAppendCache>

            Console.WriteLine($"=============== Finished Processing Data ===============");

            // <SnippetReturnPipeline>
            return pipeline;
            // </SnippetReturnPipeline>
        }

        public static ITransformer BuildAndTrainModel(
                        IDataView trainingDataView, 
                        EstimatorChain<ITransformer> pipeline,
                        MLContext mlContext)
        {
            // STEP 3: Create the training algorithm/trainer
            // Use the multi-class SDCA algorithm to predict the label using features.
            //Set the trainer/algorithm and map label to value (original readable state)
            // <SnippetAddTrainer> 
            var trainingPipeline = pipeline.Append(mlContext.MulticlassClassification.Trainers.StochasticDualCoordinateAscent(DefaultColumnNames.Label, DefaultColumnNames.Features))
                    .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
            // </SnippetAddTrainer> 

            // STEP 4: Train the model fitting to the DataSet
            Console.WriteLine($"=============== Training the model  ===============");

            // <SnippetTrainModel> 
            var trainedModel = trainingPipeline.Fit(trainingDataView);
            // </SnippetTrainModel> 
            Console.WriteLine($"=============== Finished Training the model Ending time: {DateTime.Now.ToString()} ===============");

            // (OPTIONAL) Try/test a single prediction with the "just-trained model" (Before saving the model)
            Console.WriteLine($"=============== Single Prediction just-trained-model ===============");

            // Create prediction engine related to the loaded trained model
            // <SnippetCreatePredictionEngine1>
            var predEngine = trainedModel.CreatePredictionEngine<Lyric, GenrePrediction>(mlContext);
            // </SnippetCreatePredictionEngine1>
            // <SnippetCreateTestIssue1> 
            Lyric issue = new Lyric()
            {
                Text = @"Is this the real life? Is this just fantasy?
Caught in a landslide, no escape from reality
Open your eyes, look up to the skies and see
I'm just a poor boy, I need no sympathy
Because I'm easy come, easy go, little high, little low
Any way the wind blows doesn't really matter to me, to me

Mama, just killed a man
Put a gun against his head, pulled my trigger, now he's dead
Mama, life had just begun
But now I've gone and thrown it all away
Mama, ooh, didn't mean to make you cry
If I'm not back again this time tomorrow
Carry on, carry on as if nothing really matters

Too late, my time has come
Sends shivers down my spine, body's aching all the time
Goodbye, everybody, I've got to go
Gotta leave you all behind and face the truth
Mama, ooh, (any way the wind blows)
I don't want to die
I sometimes wish I'd never been born at all

I see a little silhouetto of a man
Scaramouche, Scaramouche, will you do the Fandango?
Thunderbolt and lightning, very, very fright'ning me
(Galileo.) Galileo. (Galileo.) Galileo. Galileo Figaro magnifico
I'm just a poor boy, nobody loves me
He's just a poor boy from a poor family
Spare him his life from this monstrosity
Easy come, easy go, will you let me go?
Bismillah! No, we will not let you go
(Let him go!) Bismillah! We will not let you go
(Let him go!) Bismillah! We will not let you go
(Let me go) Will not let you go
(Let me go) Will not let you go
(Let me go) Ah
No, no, no, no, no, no, no
(Oh mamma mia, mamma mia) Mamma mia, let me go
Beelzebub has a devil put aside for me, for me, for me!

So you think you can stone me and spit in my eye?
So you think you can love me and leave me to die?
Oh, baby, can't do this to me, baby!
Just gotta get out, just gotta get right outta here!

Nothing really matters, anyone can see
Nothing really matters
Nothing really matters to me
Any way the wind blows"
            };
            // </SnippetCreateTestIssue1>

            // <SnippetPredict>
            var prediction = predEngine.Predict(issue);
            // </SnippetPredict>

            // <SnippetOutputPrediction>
            Console.WriteLine($"=============== Single Prediction just-trained-model - Result: {prediction.Genre} ===============");
            // </SnippetOutputPrediction>

            // <SnippetReturnModel>
            return trainedModel;
            // </SnippetReturnModel>

        }

    }
}
