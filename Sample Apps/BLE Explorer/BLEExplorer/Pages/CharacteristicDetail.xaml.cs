using Xamarin.Forms;
using Robotics.Mobile.Core.Bluetooth.LE;
using System.Diagnostics;
using System.Linq;
using Microsoft.Azure.Devices.Client;
using System.Text;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;

namespace BLEExplorer.Pages
{
    public class SensorData
    {
        public string Name { get; set; }
        public string Uuid { get; set; }
        public string Value { get; set; }
        public string RawValue { get; set; }

    }
    public partial class CharacteristicDetail : ContentPage
	{	
		//IAdapter adapter;
		//IDevice device;
		//IService service; 
		ICharacteristic characteristic;
        int count = 1;
        private const string DeviceConnectionString = "";
        static DeviceClient Client = null;
        public SensorData sensor = null;

        public void SendIoTData(object value)
        {
            var eventMessage = new Message(Serialize(value));
            try
            {
                Client.SendEventAsync(eventMessage).Wait();
            }
            catch (Exception ex)            {

                Debug.WriteLine("Error sending: " + ex.Message);
            }
        }
        private byte[] Serialize(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }
        public static async void InitClient()
        {
            try
            {                               
                Client = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Http1); 
                Debug.WriteLine("Creating connection");             
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in connecting: " + ex.Message);
            }

            await Client.OpenAsync();
        }

        public CharacteristicDetail (IAdapter adapter, IDevice device, IService service, ICharacteristic characteristic)
		{
			InitializeComponent ();
            InitClient();
            sensor = new SensorData();

            this.characteristic = characteristic;

            /*
            if (characteristic.CanUpdate)
            {
				characteristic.ValueUpdated += (s, e) => 
                {
					Debug.WriteLine("characteristic.ValueUpdated");

					Device.BeginInvokeOnMainThread( () => UpdateDisplay(characteristic) );
				
				};
				characteristic.StartUpdates();
			}
            */

            btnTxRx.Clicked += (s, e) => Navigation.PushAsync(new TxRx(characteristic));
            btnSent.Clicked += BtnSent_Clicked;
        }
        bool loop = false;
        private void BtnSent_Clicked(object sender, EventArgs e)
        {
            if (loop)
            {
                loop = false;
                btnSent.Text = "Start Telemetry";
            }
            else
            {
                loop = true;
                btnSent.Text = "Stop Telemetry";
            }
            Task.Factory.StartNew(LoopTelemery, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        private void LoopTelemery()
        {
            while (loop)
            {
                SendIoTData(sensor);
                Task.Delay(10000).Wait();
            }
        }

        protected override async void OnAppearing ()
		{
			base.OnAppearing ();      

            if (characteristic.CanRead) {
				var c = await characteristic.ReadAsync();
				UpdateDisplay(c);
			}
            
           
        }

		protected override void OnDisappearing() 
		{
			base.OnDisappearing();

            if (characteristic.CanUpdate) 
				characteristic.StopUpdates();
			
		}
		void UpdateDisplay (ICharacteristic c)
        {
			Name.Text = c.Name;

			ID.Text = c.ID.PartialFromUuid ();

			var s = (from i in c.Value select i.ToString ("X").PadRight(2, '0')).ToArray ();

			RawValue.Text = string.Join (":", s);

			if (c.ID == 0x2A37.UuidFromPartial ())
            {
				// heart rate
				StringValue.Text = DecodeHeartRateCharacteristicValue (c.Value);
				StringValue.TextColor = Color.Red;
			}
            else
            {
				StringValue.Text = c.StringValue;
				StringValue.TextColor = Color.Default;
			}

            sensor.Name = c.Name;
            sensor.Value = c.StringValue;
            sensor.RawValue = RawValue.Text;
            sensor.Uuid = c.Uuid;
        }

		string DecodeHeartRateCharacteristicValue(byte[] data)
        {
			ushort bpm = 0;
			if ((data [0] & 0x01) == 0)
            {
				bpm = data [1];
			}
            else
            {
				bpm = (ushort)data [1];
				bpm = (ushort)(((bpm >> 8) & 0xFF) | ((bpm << 8) & 0xFF00));
			}
			return bpm.ToString () + " bpm";
		}
	}
}