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

	private string City { get; set; }

	// [JsonPropertyName("StreetAddress")]
	[JsonProperty("StreetAddress")]
	private string Street { get; set; }

	// [JsonPropertyName("AddInfo")]
	[JsonProperty("AddInfo")]
	private string AdditionalInfo { get; set; }
}