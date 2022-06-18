using System;
using System.Net;
using System.Configuration;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using RestSharp.Authenticators;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SC4ModManager {
	public static class LEX_Access {

		//API documentation: https://github.com/caspervg/SC4Devotion-LEX-API/blob/master/documentation/Lot.md
		//C# implementation of the API: https://github.com/caspervg/SC4D-SharpLEX/blob/master/SharpLEXTests/FileTest.cs

		//API JSON result: https://www.sc4devotion.com/csxlex/api/v4/lot/2987

		public static void AccessLEXFileInfo(int LEXfileID) {
			string username = "nos.17";
			string password = "";

			RestClient client = new RestClient("https://www.sc4devotion.com/csxlex/api/v4/") {
				Authenticator = new HttpBasicAuthenticator(username, password)
			};
			RestRequest request = new RestRequest("lot/" + LEXfileID);
			RestResponse response = client.Get(request);

			if (response.IsSuccessful) {

				JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

				LEXFile file = JsonSerializer.Deserialize<LEXFile>(response.Content,options);
			}
		}
	}




	//for help creating this I used: https://json2csharp.com/
	public class LEXFile {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Version { get; set; }
		[JsonPropertyName("num_downloads")]
		public int DownloadCount { get; set; }
		public string Author { get; set; }
		[JsonPropertyName("is_exclusive")]
		public bool IsExclusive { get; set; }
		//[JsonPropertyName("maxis_category")]
		//public string BroadCategory { get; set; }
		public string Description { get; set; }
		public FileImages Images { get; set; }
		public string Link { get; set; }
		[JsonPropertyName("is_certified")]
		public bool IsCertified { get; set; } 
		[JsonPropertyName("is_active")]
		public bool IsActive { get; set; }
		public DateTime UploadDate { get; set; }
		public DateTime? UpdateDate { get; set; }
		//public DependencyOverview Dependencies { get; set; }
		public string FileSize { get; set; }
		public string FileName { get; set; }
	}

	public class FileImages {
		public string Primary { get; set; }
		public string Thumbnail { get; set; }
		public string Secondary { get; set; }
		public string Extra { get; set; }
	}

	//public class Dependency {
	//	public int Id { get; set; }
	//	//[DeserializeAs(Name = "internal")]
	//	public bool IsInternal { get; set; }
	//	public string Link { get; set; }
	//	public string Name { get; set; }
	//	public DependencyStatus Status { get; set; }
	//}

	//public class DependencyStatus {
	//	//[DeserializeAs(Name = "ok")]
	//	public bool IsOk { get; set; }
	//	//[DeserializeAs(Name = "deleted")]
	//	public bool IsDeleted { get; set; }
	//	//[DeserializeAs(Name = "locked")]
	//	public bool IsLocked { get; set; }
	//	//[DeserializeAs(Name = "superceded")]
	//	public bool IsSuperseded { get; set; }
	//	//[DeserializeAs(Name = "superceded_by")]
	//	public int Superseder { get; set; }
	//}


	//public class DependencyOverview {
	//	public string Status { get; set; }
	//	public int Count { get; set; }
	//	public List<Dependency> List { get; set; }
	//}

	//public class DependencyString {
	//	public string Dependency { get; set; }
	//}
}

