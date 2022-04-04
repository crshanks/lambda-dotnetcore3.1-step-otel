using Amazon.Lambda.Core;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Contrib.Instrumentation.AWSLambda.Implementation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace StockChecker
{
    public class StockEvent
    {
        public int stockPrice { get; set; }
        public string traceParent { get; set; }
    }

    public class Function
    {
        private static readonly Random rand = new Random((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);

        public static TracerProvider tracerProvider;

        //Provides the API for starting/stopping activities.
        private static readonly ActivitySource s_source = new ActivitySource("Sample.DistributedTracing");

        static Function()
        {
            // This switch must be set before creating the GrpcChannel/HttpClient.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MySample"))
                .AddSource("Sample.DistributedTracing")
                .AddAWSInstrumentation()
                .AddOtlpExporter()
                .AddAWSLambdaConfigurations()
                .Build();
        }

        public StockEvent TracingFunctionHandler(IDictionary<string, object> invocationEvent, ILambdaContext context)
        {
            return AWSLambdaWrapper.Trace(tracerProvider, FunctionHandler, invocationEvent, context);
        }

        public StockEvent FunctionHandler(IDictionary<string, object> invocationEvent, ILambdaContext context)
        {
            // Sample Lambda function which mocks the operation of checking the current price
            // of a stock.

            // For demonstration purposes this Lambda function simply returns
            // a random integer between 0 and 100 as the stock price.

            // Parameters
            // ----------
            // context: ILambdaContext
            //     Lambda Context runtime methods and attributes

            // Returns
            // ------
            //     StockEvent: Object containing the current price of the stock

            string functionName = context.FunctionName != null ? context.FunctionName : "Invoke";
            // Note that invocationEvent["detail"] returns a JsonElement. There must be a more efficient way to access traceParent!
            string traceParent = System.Text.Json.JsonDocument.Parse(invocationEvent["detail"].ToString()).RootElement.GetProperty("traceParent").ToString();
            LambdaLogger.Log("traceParent: " + traceParent);

            //create root-span, connecting with trace-parent read from the invocationEvent
            using (var activity = s_source.StartActivity(functionName, ActivityKind.Server, traceParent))
            {
                //.....
                //... YOUR CODE GOES HERE
                //....
                LambdaLogger.Log("ENVIRONMENT VARIABLES: " + JsonConvert.SerializeObject(System.Environment.GetEnvironmentVariables()));
                LambdaLogger.Log("CONTEXT: " + JsonConvert.SerializeObject(context));
                LambdaLogger.Log("EVENT: " + JsonConvert.SerializeObject(invocationEvent));
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

                return new StockEvent
                {
                    stockPrice = rand.Next() % 100,
                    traceParent = activity.Id
                };
            }
        }
    }
}
