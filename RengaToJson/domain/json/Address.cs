using Newtonsoft.Json;

namespace RengaToJson.domain;

public class Address
{
	public Address(string city, string street, string additionalInfo)
	{
		City = city;
		Street = street;
		AdditionalInfo = additionalInfo;
	}

	public string City { get; set; }
	// [JsonPropertyName("StreetAddress")]
	[JsonProperty("StreetAddress")]
	public string Street { get; set; }
	// [JsonPropertyName("AddInfo")]
	[JsonProperty("AddInfo")]
	public string AdditionalInfo { get; set; }
}