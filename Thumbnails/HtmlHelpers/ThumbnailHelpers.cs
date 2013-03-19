using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thumbnails.Configuration;
using System.Configuration;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Web.Routing;

namespace Thumbnails.HtmlHelpers
{
	public static class ThumbnailHelpers
	{
		#region Global Variables

		private static ThumbnailSettings settings = (ThumbnailSettings)ConfigurationManager.GetSection("thumbnailSettings");

		#endregion

		#region HtmlHelper Extension Methods

		/// <summary>
		/// Returns a resized img element for the specified image using the size defined in the alias
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="image">The filename of the image needed for the thumbnail</param>
		/// <param name="alias">The name of the alias defined in web.config</param>
		/// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element</param>
		/// <returns>MvcHtmlString of the thumbnail img element</returns>
		public static MvcHtmlString Thumbnail(this HtmlHelper helper, string image, string alias, object htmlAttributes)
		{
			//find the alias requested in the web.config settings
			ThumbnailSettingsAlias requestedAlias = settings.Aliases.Single(a => a.Name.ToLower() == alias.ToLower());

			TagBuilder img = GetThumbnailTag(helper, requestedAlias.FolderName, image, requestedAlias.Width, requestedAlias.Height);

			//Merge any additional html attributes passed in
			RouteValueDictionary additionalAttributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			img.MergeAttributes(additionalAttributes);

			return new MvcHtmlString(img.ToString());
		}

		/// <summary>
		/// Returns a resized img element for the specified image using the width and height requested
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="image">The filename of the image needed for the thumbnail</param>
		/// <param name="width">Width of the thumbnail</param>
		/// <param name="height">Height of the thumbnail</param>
		/// <param name="htmlAttributes">An object that contains the HTML attributes to set for the element</param>
		/// <returns>MvcHtmlString of the thumbnail img element</returns>
		public static MvcHtmlString Thumbnail(this HtmlHelper helper, string image, int width, int height, object htmlAttributes)
		{
			String folderName = String.Format("{0}x{1}", width, height);

			TagBuilder img = GetThumbnailTag(helper, folderName, image, width, height);

			//Merge any additional html attributes passed in
			RouteValueDictionary additionalAttributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
			img.MergeAttributes(additionalAttributes);
			return new MvcHtmlString(img.ToString());
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Returns an img TagBuilder object with the src set to the requested thumbnail
		/// </summary>
		/// <param name="helper">The HtmlHelper instance (from the extension methods)</param>
		/// <param name="folderName">The name of the folder where this thumbnail should be saved inside the Thumbnails directory</param>
		/// <param name="image">The image filename (should include file extension)</param>
		/// <param name="width">The desired width of the thumbnail</param>
		/// <param name="height">The desired height of the thumbnail</param>
		/// <returns>An img TagBuilder object with the src attribute set to the source of the thumbnail</returns>
		private static TagBuilder GetThumbnailTag(HtmlHelper helper, string folderName, string image, int width, int height)
		{
			TagBuilder img = new TagBuilder("img");
			bool originalMissing = false;
			//the physical path to the directory defined in baseImagePath in web.config
			String baseImagePhysicalPath = helper.ViewContext.HttpContext.Server.MapPath(settings.BaseImagePath);
			//location of the original image
			String originalImagePhysicalPath = Path.Combine(baseImagePhysicalPath, image);

			//if the original image doesn't exist use the missing image defined in web.config
			if (!File.Exists(originalImagePhysicalPath))
			{
				originalImagePhysicalPath = helper.ViewContext.HttpContext.Server.MapPath(settings.MissingImagePath);
				originalMissing = true;
			}
			//Will be of the form C://path/to/baseImagePath/Thumbnails/AliasFolderName
			String physicalThumbnailDir = Path.Combine(baseImagePhysicalPath, "Thumbnails", folderName);

			//If the original image was not found then use the settings.MissingImagePath filename - otherwise use the image requested
			String thumbnailPath = Path.Combine(physicalThumbnailDir, originalMissing ? Path.GetFileName(originalImagePhysicalPath) : image);
			//Will be /baseImagePath/Thumbnails/AliasFolderName/image.ext
			String virtualThumbnailPath = String.Concat(VirtualPathUtility.ToAbsolute(settings.BaseImagePath),
				"/Thumbnails/", folderName, "/", originalMissing ? Path.GetFileName(originalImagePhysicalPath) : image);

			//if the requested thumbnail does not exist then create it
			if (!File.Exists(thumbnailPath))
			{
				//create the necessary directories
				//There will always be a Thumbnails directory in the dir defined in baseImagePath
				//Inside the Thumbnails directory there will be a directory for each alias
				if (!Directory.Exists(physicalThumbnailDir))
					Directory.CreateDirectory(physicalThumbnailDir);

				Image originalImage = Image.FromFile(originalImagePhysicalPath);
				CreateThumbnail(originalImage, thumbnailPath, new Size(width, height));
			}

			//set the image source and return it
			img.MergeAttribute("src", virtualThumbnailPath);
			return img;
		}

		/// <summary>
		/// Crops and resizes the original image (if necessary) and saves it to disk
		/// </summary>
		/// <param name="originalImage">The original image that needs to be cropped/resized</param>
		/// <param name="savePath">The location for the resized image to be saved - This should be a full physical path including the filename and extension</param>
		/// <param name="size">A Size object containing the desired width and height of the thumbnail</param>
		private static void CreateThumbnail(Image originalImage, string savePath, Size size)
		{
			Image source;

			//this image is already the correct size - don't just copy it into the new directory
			if (originalImage.Width == size.Width && originalImage.Height == size.Height)
			{
				source = (Image)originalImage.Clone();
				source.Save(savePath);
				return;
			}

			if (originalImage.Width <= size.Width && originalImage.Height <= size.Height)
			{
				source = (Image)originalImage.Clone();
				source.Save(savePath);
				return;
			}

			//The image needs to be cropped before it can be resized
			//TODO: there is probably a better way to crop the image
			if (originalImage.Width != originalImage.Height)
			{
				int sizeToUse = originalImage.Width > originalImage.Height ? originalImage.Height : originalImage.Width;
				Rectangle cropRect = new Rectangle(0, 0, sizeToUse, sizeToUse);
				source = new Bitmap(cropRect.Width, cropRect.Height);

				//Set the source to the new cropped image
				using (Graphics gr = Graphics.FromImage(source))
				{
					gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
					gr.DrawImage(originalImage, new Rectangle(0, 0, source.Width, source.Height), cropRect, GraphicsUnit.Pixel);
				}
			}
			else
			{
				//originalImage is a perfect square so no need to crop
				source = originalImage;
			}

			//resize the image
			Image thumbnail = new Bitmap(size.Width, size.Height);
			using (Graphics gr = Graphics.FromImage(thumbnail))
			{
				gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
				gr.DrawImage(source, 0, 0, size.Width, size.Height);
			}
			thumbnail.Save(savePath);
		}
		#endregion
	}
}