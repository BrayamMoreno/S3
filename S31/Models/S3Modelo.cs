namespace S31.Models
{
	public class S3Modelo
	{
		public string? Key { get; set; }
		public DateTime LastModified { get; set; }
		public long Size { get; set; }
		public string? Bucket { get; set; }
	}

	public class S3ModeloGroup
	{
		public string? GroupName { get; set; }
		public List<S3Modelo> Files { get; set; }
	}


}
