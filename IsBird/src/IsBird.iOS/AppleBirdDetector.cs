using System;
using Foundation;
using IsBird.Interfaces;
using IsBird.iOS;
using Xamarin.Forms;
using Plugin.Media.Abstractions;
using System.Diagnostics;
using Vision;
using CoreML;

[assembly: Dependency(typeof(AppleBirdDetector))]
namespace IsBird.iOS
{
    public class AppleBirdDetector : IBirdDetector
    {
        //properties and variables
        VNCoreMLModel model;
        VNRequest[] classificationRequests;
        public Tuple<string, string> picResults { get; set; }

        //Method to satisfy IBirdDetector interface...
        public Tuple<string, string> GetBirdResults(MediaFile file)
        {
            try
            {
                CollectImageData(file);
                return picResults;
            } 
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        public void CollectImageData(MediaFile file)
        {
            var imagedata = NSData.FromStream(file.GetStream());
            //VNIImageOptions is a dictionary container used to hold options involved with vision queries
            var requestHandler = new VNImageRequestHandler(imagedata, new VNImageOptions());
            requestHandler.Perform(ClassificationRequest, out NSError error);

            if (error != null)
                Debug.WriteLine($"Error identifying... {error}");
        }

        public VNRequest[] ClassificationRequest
        {
            get
            {
                if (model == null)
                {
                    //get path to resource (MLModel) we will use
                    var modelPath = NSBundle.MainBundle.GetUrlForResource("TheseAreBirds", "mlmodel");
                    var compiledPath = MLModel.CompileModel(modelPath, out NSError compileError);           //compile it
                    var mlModel = MLModel.Create(compiledPath, out NSError createError);   
                    //Wraps the coreML model for use with vision services
                    model = VNCoreMLModel.FromMLModel(mlModel, out NSError mlError);    
                }

                if (classificationRequests == null)
                {
                    var classificationRequest = new VNCoreMLRequest(model, HandleClassificationRequest);
                    classificationRequests = new[] { classificationRequest };
                }
                return classificationRequests;
            }
        }

        public void HandleClassificationRequest(VNRequest request, NSError error)
        {
            var observations = request.GetResults<VNClassificationObservation>();
            //get the first (#1 result)
            var best = observations?[0];

            //build a tuple to return
            var bestTag = best.Identifier.Trim();
            var confidence = $"{best.Confidence:P0}";
            picResults = new Tuple<string, string>(bestTag, confidence);
        }


    }
}
