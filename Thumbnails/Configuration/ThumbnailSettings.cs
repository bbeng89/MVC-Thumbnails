using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace Thumbnails.Configuration
{
	public class ThumbnailSettings : ConfigurationSection
	{
		private static ThumbnailSettings settings = ConfigurationManager.GetSection("thumbnailSettings") as ThumbnailSettings;

		public static ThumbnailSettings Settings
		{
			get { return settings; }
		}

		[ConfigurationProperty("baseImagePath", DefaultValue = "~/Content/img", IsRequired = false)]
		public string BaseImagePath
		{
			get { return (string)this["baseImagePath"]; }
		}

		[ConfigurationProperty("missingImagePath", DefaultValue = "~/Content/img/missing.jpg", IsRequired = false)]
		public string MissingImagePath
		{
			get { return (string)this["missingImagePath"]; }
		}

		[ConfigurationProperty("aliases", IsRequired=false)]
		public ThumbnailSettingsAliasCollection Aliases
		{
			get
			{
				return this["aliases"] as ThumbnailSettingsAliasCollection;
			}
		}
	}

	public class ThumbnailSettingsAlias : ConfigurationElement
	{
		[ConfigurationProperty("name", IsRequired=true)]
		public string Name
		{
			get{ return this["name"] as string; }
		}

		[ConfigurationProperty("width", IsRequired=true)]
		public int Width
		{
			get{ return (int)this["width"]; }
		}

		[ConfigurationProperty("height", IsRequired=true)]
		public int Height
		{
			get{ return (int)this["height"]; }
		}

		public string FolderName
		{
			get { return String.Format("{0}-{1}x{2}", this["name"], this["width"], this["height"]); }
		}
	}

	public class ThumbnailSettingsAliasCollection : ConfigurationElementCollection, IEnumerable<ThumbnailSettingsAlias>
	{
		public ThumbnailSettingsAlias this[int index]
		{
			get
			{
				return base.BaseGet(index) as ThumbnailSettingsAlias;
			}
			set
			{
				if(base.BaseGet(index) != null)
				{
					base.BaseRemoveAt(index);
				}
				this.BaseAdd(index, value);
			}
		}

		protected override ConfigurationElement  CreateNewElement()
		{
 			return new ThumbnailSettingsAlias();
		}

		protected override object  GetElementKey(ConfigurationElement element)
		{
 			return ((ThumbnailSettingsAlias)element).Name;
		}

		public new IEnumerator<ThumbnailSettingsAlias> GetEnumerator()
		{
			int count = base.Count;
			for (int i = 0; i < count; i++)
			{
				yield return base.BaseGet(i) as ThumbnailSettingsAlias;
			}
		}
	}
}