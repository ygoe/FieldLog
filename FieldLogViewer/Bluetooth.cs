using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace Unclassified.FieldLogViewer
{
	class Bluetooth
	{
		private void SendFile()
		{
			BluetoothRadio radio = BluetoothRadio.PrimaryRadio;
			BluetoothClient bluetoothClient = new BluetoothClient();
			//BluetoothAddress addr = BluetoothAddress.Parse("0017EA7E8B9E");
			Uri uri = new Uri("obex://0017EA7E8B9E/mantis_logo.png");

			ObexWebRequest request = new ObexWebRequest(uri);
			request.ReadFile(@"C:\Users\yves\Downloads\mantis_logo.png");

			ObexWebResponse response = request.GetResponse() as ObexWebResponse;
			//MessageBox.Show(response.StatusCode.ToString());
			response.Close();
		}

		private string ScanDevices()
		{
			string str = "";
			BluetoothRadio radio = BluetoothRadio.PrimaryRadio;
			BluetoothClient bluetoothClient = new BluetoothClient();
			BluetoothDeviceInfo[] bluetoothDevices = bluetoothClient.DiscoverDevices();
			foreach (BluetoothDeviceInfo bdi in bluetoothDevices)
			{
				str += bdi.ClassOfDevice.Device + ": " + bdi.DeviceName + ", Address=" + bdi.DeviceAddress + Environment.NewLine;
			}
			return str;
		}
	}
}
