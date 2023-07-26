using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using S31.Models;
using System.Security.Claims;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Amazon.S3.Transfer;
using System.IO;
using Amazon;

namespace S31.Controllers
{
	[Authorize]
	public class S31Controller : Controller
	{
		private static string? Cubeta2,Nombre2,Contraseña2; 
		private readonly IAmazonS3 _s3Client;

		public S31Controller(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        private Dictionary<string, List<S3Modelo>> GroupFilesByFolder(IEnumerable<S3Object> s3Objects)
        {
            var groupedFiles = new Dictionary<string, List<S3Modelo>>();

            foreach (var file in s3Objects)
            {
                var parts = file.Key.Split('/');
                if (parts.Length >= 3) 
                {
                    var groupName = parts[2];
                    if (!groupedFiles.ContainsKey(groupName))
                    {
                        groupedFiles[groupName] = new List<S3Modelo>();
                    }
                    groupedFiles[groupName].Add(new S3Modelo
                    {
                        Key = file.Key,
                        LastModified = file.LastModified,
                        Size = file.Size,
                        Bucket = file.BucketName
                    });
                }
                else
                {
                    var groupName = "Sin Carpeta";
                    if (!groupedFiles.ContainsKey(groupName))
                    {
                        groupedFiles[groupName] = new List<S3Modelo>();
                    }
                    groupedFiles[groupName].Add(new S3Modelo
                    {
                        Key = file.Key,
                        LastModified = file.LastModified,
                        Size = file.Size,
                        Bucket = file.BucketName
                    });
                }
            }

            return groupedFiles;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var httpContext = HttpContext;
                string nombre = httpContext.User.FindFirstValue("Nombre");
                string cubeta = httpContext.User.FindFirstValue("Cubeta");
                string contraseña = httpContext.User.FindFirstValue("Contraseña");

                using (var _s3Client = new AmazonS3Client(nombre, contraseña, Amazon.RegionEndpoint.USEast1))
                {
                    var listRequest = new ListObjectsV2Request
                    {
                        BucketName = cubeta
                    };

                    var response = await _s3Client.ListObjectsV2Async(listRequest);
                    var s3Objects = response.S3Objects;

                    var groupedFiles = GroupFilesByFolder(s3Objects);

                    var S3Modelo = groupedFiles.Select(group => new S3ModeloGroup
                    {
                        GroupName = group.Key,
                        Files = group.Value
                    }).ToList();

                    return View(S3Modelo);
                }
            }
            catch (AmazonS3Exception ex)
            {
                TempData["ErrorCredenciales"] = "Error al iniciar, verifique sus credenciales.";
                return RedirectToAction("Index", "LogIn");
            }
        }

        private int MAX_UPLOAD_SIZE = 1024 * 1024 * 64;
        private object fileTransferUtility;

        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var httpContext = HttpContext;
                string nombre = httpContext.User.FindFirstValue("Nombre");
                string cubeta = httpContext.User.FindFirstValue("Cubeta");
                string contraseña = httpContext.User.FindFirstValue("Contraseña");
                string key = file.FileName;

                try
                {
                    var config = new AmazonS3Config
                    {
                        RegionEndpoint = RegionEndpoint.USEast1
                    };

                    var sing = AWSConfigsS3.UseSignatureVersion4;
                    AWSConfigsS3.UseSignatureVersion4 = sing;

                    using (var client = new AmazonS3Client(nombre, contraseña, config))
                    {
                        using (var transferUtility = new TransferUtility(client))
                        {
                            if (file.Length > MAX_UPLOAD_SIZE)
                            {
                                var uploadRequest = new TransferUtilityUploadRequest
                                {
                                    BucketName = cubeta,
                                    Key = key,
                                    InputStream = file.OpenReadStream(),
                                    PartSize = 1024 * 1024 * 64,
                                    StorageClass = S3StorageClass.Standard
                                };

                                await transferUtility.UploadAsync(uploadRequest);
                            }
                            else
                            {
                                await transferUtility.UploadAsync(file.OpenReadStream(), cubeta, key);
                            }
                        }
                    }

                    TempData["Status"] = "El archivo se cargó correctamente";
                    return RedirectToAction("Dashboard");
                }
                catch (AmazonS3Exception e)
                {
                    TempData["Status"] = "Error al subir el archivo a Amazon S3: " + e.Message;
                    return RedirectToAction("Dashboard");
                }
            }
            else
            {
                TempData["Status"] = "No hay ningún archivo para cargar";
                return RedirectToAction("Dashboard");
            }
        }


        public async Task<IActionResult> Download(string key)
        {
            var httpContext = HttpContext;
            string nombre = httpContext.User.FindFirstValue("Nombre");
            string cubeta = httpContext.User.FindFirstValue("Cubeta");
            string contraseña = httpContext.User.FindFirstValue("Contraseña");


            var config = new AmazonS3Config();

            config.RegionEndpoint = RegionEndpoint.USEast1;

            var sing = AWSConfigsS3.UseSignatureVersion4;
            AWSConfigsS3.UseSignatureVersion4 = sing;

       
            var _s3Client = new AmazonS3Client(nombre, contraseña, config);

            var request = new GetObjectRequest()
            {
               
                BucketName = cubeta,
                Key = key
            };

            using GetObjectResponse response = await _s3Client.GetObjectAsync(request);

            var presignedUrlRequest = new GetPreSignedUrlRequest
            {
                    BucketName = cubeta,
                    Key = key,
                    Expires = DateTime.Now.AddHours(1)
            };
            
            string presignedUrl = _s3Client.GetPreSignedURL(presignedUrlRequest);
            return Redirect(presignedUrl);
               
        }
   
    }
}

