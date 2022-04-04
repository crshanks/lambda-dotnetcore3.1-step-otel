using Amazon.Lambda.Core;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Contrib.Instrumentation.AWSLambda.Implementation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace StockBuyer
{
    public class StockEvent
    {
        public int stockPrice { get; set; }
        public string traceParent { get; set; }
    }

    public class TransactionResult
    {
        public string id { get; set; }
        public string price { get; set; }
        public string type { get; set; }
        public string qty { get; set; }
        public string timestamp { get; set; }
    }

    public class Function
    {

        private static readonly Random rand = new Random((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);

        public static TracerProvider tracerProvider;

        //Provides the API for starting/stopping activities.
        private static readonly ActivitySource s_source = new ActivitySource("Sample.DistributedTracing");

        static Function()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MySample"))
                .AddSource("Sample.DistributedTracing")
                .AddAWSInstrumentation()
                .AddOtlpExporter()
                .AddAWSLambdaConfigurations()
                .Build();
        }

        public TransactionResult TracingFunctionHandler(StockEvent stockEvent, ILambdaContext context)
        {
            return AWSLambdaWrapper.Trace(tracerProvider, FunctionHandler, stockEvent, context);
        }

        public TransactionResult FunctionHandler(StockEvent stockEvent, ILambdaContext context)
        {
            // Sample Lambda function which mocks the operation of buying a random number
            // of shares for a stock.

            // For demonstration purposes, this Lambda function does not actually perform any
            // actual transactions. It simply returns a mocked result.

            // Parameters
            // ----------
            // stockEvent: StockEvent, required
            //     Input event to the Lambda function

            // context: ILambdaContext
            //     Lambda Context runtime methods and attributes

            // Returns
            // ------
            //     TransactionResult: Object containing details of the stock buying transaction

            string functionName = context.FunctionName != null ? context.FunctionName : "Invoke";
            // Note that invocationEvent["detail"] returns a JsonElement. There must be a more efficient way to access traceParent!
            string traceParent = stockEvent.traceParent;
            LambdaLogger.Log("traceParent: " + traceParent);

            //create root-span, connecting with trace-parent read from the invocationEvent
            using (var activity = s_source.StartActivity(functionName, ActivityKind.Server, traceParent))
            {
                //.....
                //... YOUR CODE GOES HERE
                //....
                LambdaLogger.Log("ENVIRONMENT VARIABLES: " + JsonConvert.SerializeObject(System.Environment.GetEnvironmentVariables()));
                LambdaLogger.Log("CONTEXT: " + JsonConvert.SerializeObject(context));
                LambdaLogger.Log("EVENT: " + JsonConvert.SerializeObject(stockEvent));
                LambdaLogger.Log("activity.TraceId: " + activity.TraceId);
                LambdaLogger.Log("activity.Id: " + activity.Id);
                LambdaLogger.Log("activity.DisplayName: " + activity.DisplayName);
                LambdaLogger.Log("activity.IdFormat: " + activity.IdFormat);
                LambdaLogger.Log("activity.SpanId: " + activity.SpanId);
                LambdaLogger.Log("activity.RootId: " + activity.RootId);
                LambdaLogger.Log("activity.OperationName: " + activity.OperationName);
                LambdaLogger.Log("activity.ParentId: " + activity.ParentId);
                LambdaLogger.Log("activity.ParentSpanId: " + activity.ParentSpanId);
                LambdaLogger.Log("activity.ActivityTraceFlags: " + activity.ActivityTraceFlags);

                return new TransactionResult
                {
                    id = rand.Next().ToString(),
                    type = "Buy",
                    price = stockEvent.stockPrice.ToString(),
                    qty = (rand.Next() % 10 + 1).ToString(),
                    timestamp = DateTime.Now.ToString("yyyyMMddHHmmssffff")
                };
            }
        }
    }
}
