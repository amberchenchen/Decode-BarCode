using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AVFoundation;
using CoreGraphics;
using Foundation;
using Newtonsoft.Json;
using UIKit;

namespace decodeBarCodeFromWebApi
{
	public partial class ViewController : UIViewController
	{
		UIImagePickerController picker;
		partial void UIButton3_TouchUpInside(UIButton sender)
		{
			picker = new UIImagePickerController();
			picker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
			picker.MediaTypes = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.PhotoLibrary);
			picker.FinishedPickingMedia += Finished;
			picker.Canceled += Canceled;
			PresentViewController(picker, animated: true, completionHandler: null);
		}

		public async void Finished(object sender, UIImagePickerMediaPickedEventArgs e)
		{
			textView.Text = "";

			bool isImage = false;
			switch (e.Info[UIImagePickerController.MediaType].ToString())
			{
				case "public.image":
					isImage = true;
					break;
				case "public.video":
					break;
			}
			NSUrl referenceURL = e.Info[new NSString("UIImagePickerControllerReferenceURL")] as NSUrl;
			NSUrl mediaURL = null;
			//e.Info[new NSString("UIImagePickerControllerReferenceUrl")] as NSUrl;
			UIImage originalImage = new UIImage();
			if (referenceURL != null) Console.WriteLine("Url:" + referenceURL.ToString());
			if (isImage)
			{
				originalImage = e.Info[UIImagePickerController.OriginalImage] as UIImage;
				if (originalImage != null)
				{
					//originalImage.Scale(imgView.Frame.Size);
					//imgView.Image = originalImage;
					var imageView = new UIImageView(originalImage);
					imageView.Frame = new CoreGraphics.CGRect(30, 150, 300,200);
					View.AddSubview(imageView);

					//resize image
					var width = originalImage.Size.Width;
					var height = originalImage.Size.Height;
					var newWidth = 500;
					var newHeigth = height * newWidth / width;
					UIGraphics.BeginImageContext(new SizeF(newWidth, (float)newHeigth));
					originalImage.Draw(new RectangleF(0, 0, newWidth, (float)newHeigth));
					originalImage = UIGraphics.GetImageFromCurrentImageContext();
					UIGraphics.EndImageContext();
				}
			}
			else {
				 mediaURL = e.Info[UIImagePickerController.MediaURL] as NSUrl;
				if (mediaURL != null)
				{
					AVPlayer avPlayer;
					AVPlayerLayer playerLayer;
					AVAsset asset;
					AVPlayerItem playerItem;
					asset = AVAsset.FromUrl(mediaURL);
					playerItem = new AVPlayerItem(asset);
					avPlayer = new AVPlayer(playerItem);
					playerLayer = AVPlayerLayer.FromPlayer(avPlayer);
					playerLayer.Frame = new CGRect(30, 150, 250, 250);
					View.Layer.AddSublayer(playerLayer);
					avPlayer.Play();
				}
			}
			picker.DismissModalViewController(true);

			Stream stream = null;
			//string encodedString = imgView.Image.AsPNG().GetBase64EncodedString(NSDataBase64EncodingOptions.None);


			string TodyTime = DateTime.Now.ToString("yyyyMMddHHmmss");
			String fileName = null;

			//upload image to server
			if (isImage)
			{
				stream = originalImage.AsPNG().AsStream();
				fileName = "Img" + TodyTime + ".png";
			}
			else
			{
				//convert video to stream
				NSData video = NSData.FromUrl(mediaURL);
				stream = video.AsStream();
				fileName = "Video" + TodyTime + ".mp4";
			}

			statusLable.Text = "upload file...";

			var isUplpad = await Task.Run(() => PostPicture(stream,fileName));

			//decode image
			string data = null;
			if (isUplpad.Equals("True")) 
			{
				statusLable.Text = "decoding...";
				data = await Task.Run(() => getBarCodeText(fileName));
			}

			statusLable.Text = "Finish!";

			if (String.IsNullOrEmpty(data))
			{
				textView.Text = "Bad Image";
			}
			else 
			{
				textView.Text = data;
			}
		}

		public void Canceled(object sender, EventArgs e)
		{
			picker.DismissModalViewController(true);
		}

		protected ViewController(IntPtr handle) : base(handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.
		}


		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}

		public async Task<String> getBarCodeText(string fileName)
		{
			//var content = new FormUrlEncodedContent(new[]
			//{
			//	new KeyValuePair<string, string>("base64String", base64String)
			//});
			var postData = new Dictionary<string, string>();
			postData.Add("fileName", fileName);

			var jsonString = JsonConvert.SerializeObject(postData);

			var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

			HttpClient client = new HttpClient();
			client.Timeout = TimeSpan.FromMinutes(30);
			var response = await client.PostAsync("http://app.usa543.com:5000/", content);
			var result = response.Content.ReadAsStringAsync().Result;
			return result;
		}

		public async Task<string> PostPicture(Stream stream,string fileName)
		{
			var content = new MultipartFormDataContent();
			var streamContent = new StreamContent(stream);
			streamContent.Headers.Add("Content-Type", "application/octet-stream");
			streamContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"" + fileName + "\"");
			content.Add(streamContent, "file", fileName);

			var httpClient = new HttpClient();

			var uploadServiceBaseAddress = "http://app.usa543.com:5000/Home/upload";

			var httpResponseMessage = await httpClient.PostAsync(uploadServiceBaseAddress, content);
			var text = await httpResponseMessage.Content.ReadAsStringAsync();

			return text;
		}
	}
}
