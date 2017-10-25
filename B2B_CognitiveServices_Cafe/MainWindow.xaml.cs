using B2B_CognitiveServices_Cafe.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace B2B_CognitiveServices_Cafe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<CoffeeOrder> Orders { get; set; }

        public MainWindow()
        {
            Orders = new ObservableCollection<CoffeeOrder>();
            InitializeComponent();
            DataContext = this;
        }
        
        private async void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            Orders.Clear();
            var coffeeOrderResult = await MakeLuisRequest(InputTextBox.Text);
            await ShowCoffeeOrder(coffeeOrderResult);
        }
        
        private async Task ShowCoffeeOrder(CoffeeOrderResult coffeeOrderResult)
        {
            if (coffeeOrderResult != null)
            {
                Dispatcher.Invoke(() =>
                {
                    Orders.Clear();
                    InputTextBox.Text = string.Empty;
                    foreach (var coffeeOrder in coffeeOrderResult.Order)
                    {
                        Orders.Add(coffeeOrder);
                    }
                    ResultTextBox.Text = coffeeOrderResult.JsonResponse;
                });
            }
        }









        //--------------------------------------------------------------------
        // 1 - LUIS


        // --------------------------------------
        // Edit these values
        // --------------------------------------
        private const string LuisAppId = "<Your LUIS App Id>";
        private const string LuisSubscriptionKey = "<Your Luis Subscription Id>";




        public async Task<CoffeeOrderResult> MakeLuisRequest(string orderText)
        {
            string apiResponse = "";

            ////////////////////////////////////////////////////////////////
            // INSERT LUIS API CALL CODE HERE!
            ////////////////////////////////////////////////////////////////

            var result = JsonConvert.DeserializeObject<LuisModel>(apiResponse);

            var order = ProcessOrder(result);

            return new CoffeeOrderResult
            {
                JsonResponse = apiResponse,
                Order = order
            };
        }

        private IEnumerable<CoffeeOrder> ProcessOrder(LuisModel result)
        {
            var orders = new List<CoffeeOrder>();
            try
            {
                foreach (var compositeEntity in result.compositeEntities)
                {
                    try
                    {
                        var coffeeOrder = compositeEntity.ToCoffeeOrder(result.entities);
                        if (coffeeOrder != null)
                        {
                            orders.Add(coffeeOrder);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Caught an error processing compositeEntity: " + e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an error processing LuisModel: " + e);
            }

            return orders;
        }




        /*--------------------------------------------------------------------
        // 2 - Speech Recognition


        // --------------------------------------
        // Edit these values
        // --------------------------------------
        private const string BingSpeechSubscriptionKey = "<Your Bing Speech API Key>";


        private MicrophoneRecognitionClient micClient;


        private void ListenToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (micClient == null)
            {
                CreateCustomMicrophoneClientWithIntent();
            }

            micClient.StartMicAndRecognition();
        }


        private void CreateMicrophoneClientWithIntent()
        {    
            ////////////////////////////////////////////////////////////////
            // INSERT SPEECH TO TEXT API CALL CODE HERE!     
            ////////////////////////////////////////////////////////////////

            RegisterMicrophoneEventHandlers();
        }

        private void RegisterMicrophoneEventHandlers()
        {
            micClient.OnIntent += OnIntentHandler;
            micClient.OnMicrophoneStatus += OnMicrophoneStatus;
            micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            micClient.OnConversationError += OnConversationErrorHandler;
        }


        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                InputTextBox.Text = e.PartialResult;
            });
        }

        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                InputTextBox.Text += $"    ERROR: {e.SpeechErrorText}";
            });

        }

        private async void OnIntentHandler(object sender, SpeechIntentEventArgs e)
        {
            // Stop the microphone
            micClient.EndMicAndRecognition();

            string apiResponse = e.Payload;
            var result = JsonConvert.DeserializeObject<LuisModel>(apiResponse);
            var order = ProcessOrder(result);

            await ShowCoffeeOrder(new CoffeeOrderResult
            {
                JsonResponse = apiResponse,
                Order = order
            });
        }

        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Recording)
                {
                    InputTextBox.Text = "Listening...";
                }
                else
                {
                    InputTextBox.Text += " Placing Order...";
                }
            });
        }

        --------------------------------------------------------------------- */


        /*--------------------------------------------------------------------
        // 2b - Custom Speech Recognition
        //
        // By building a custom speech model we can get much better results


        // --------------------------------------
        // These values point to casey's custom speech endpoint. 
        // If you have time, you can create your own endpoint 
        // and update them to your own values.
        // --------------------------------------
        private const string CustomSpeechApiPrimaryKey = "1fe791132e4446fbb34a81416c94ba3d";
        private const string CustomSpeechApiSecondaryKey = "83cb3abfeac041958f8216c9648fc8da";
        private const string CustomSpeechApiUrl = "https://54eddef2177945bfa631ea5b31a4a1a4.api.cris.ai/ws/cris/speech/recognize";
        private const string CustomSpeechAuthorizationUrl = "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken";

        // Swap the ListenToOrderButton_Click handler to call this method instead of the basic one.

        private void CreateCustomMicrophoneClientWithIntent()
        {
            ////////////////////////////////////////////////////////////////
            // INSERT CUSTOM SPEECH TO TEXT API CALL CODE HERE!          
            ////////////////////////////////////////////////////////////////

            micClient.AuthenticationUri = CustomSpeechAuthorizationUrl;

            RegisterMicrophoneEventHandlers();
        }

       // ----------------------------------------------------------------------



        /*--------------------------------------------------------------------
        // 3 - Text to Speech Confirmation
        
        private static void PlayAudio(object sender, GenericEventArgs<Stream> args)
        {
            SoundPlayer player = new SoundPlayer(args.EventData);
            player.PlaySync();
            args.EventData.Dispose();
        }

        private static void ErrorHandler(object sender, GenericEventArgs<Exception> e)
        {
            Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
        }
        
        private string FormatOrderText(CoffeeOrderResult order)
        {
            string orderConfirmation = "";

            if (order.Order.Any())
            {
                var intros = new string[] { "No worries", "Sure", "Good choice" };
                var outros = new string[] { "coming right up", "on the way", ". That won't be long" };

                orderConfirmation = $"{intros.Random()}. ";
                foreach (var orderItem in order.Order)
                {
                    orderConfirmation += $"{orderItem.Number} {orderItem.CoffeeType}, ";
                }
                orderConfirmation = orderConfirmation.TrimEnd(new[] { ',', ' '}) + $" {outros.Random()}. ";
            }
            else
            {
                var noOrderResponses = new string[] 
                {
                    "I'm sorry, I didn't quite catch that.",
                    "Could you please repeat your order"
                };

                orderConfirmation = noOrderResponses.Random();
            }

            return orderConfirmation;
        }

        private async Task ConfirmOrderVerbally(CoffeeOrderResult order)
        {
            string accessToken;
            
            Authentication auth = new Authentication(BingSpeechSubscriptionKey);

            try
            {
                accessToken = auth.GetAccessToken();
            }
            catch (Exception)
            {
                Console.WriteLine("Failed authentication.");
                return;
            }

            var orderConfirmationText = FormatOrderText(order);
            
            var cortana = new Cortana();
            
            cortana.OnAudioAvailable += PlayAudio;
            cortana.OnError += ErrorHandler;
            
            ////////////////////////////////////////////////////////////////
            // INSERT TEXT TO SPEECH CODE HERE!
            ////////////////////////////////////////////////////////////////
        }
        
        ----------------------------------------------------------------------*/
    }
}
