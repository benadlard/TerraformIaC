﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Extensions.WebEncoders;
using PartsUnlimited.WebsiteConfiguration;
using System;
using System.IO;
using System.Text;

namespace PartsUnlimited
{
    public static class ContentDeliveryNetworkExtensions
    {
        public static IContentDeliveryNetworkConfiguration Configuration { get; set; }

        public static IHtmlContent Image(this IHtmlHelper helper, string src, string alt = null)
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                throw new ArgumentOutOfRangeException(nameof(src), src, "Must not be null or whitespace");
            }

            var img = new TagBuilder("img");

            img.MergeAttribute("src", GetCdnSource(src));

            if (!string.IsNullOrWhiteSpace(alt))
            {
                img.MergeAttribute("alt", alt);
            }

            img.TagRenderMode = TagRenderMode.SelfClosing;
            return img;
        }

        public static IHtmlContent ProductImage(this IHtmlHelper helper, string src, string alt = null)
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                throw new ArgumentOutOfRangeException(nameof(src), src, "Must not be null or whitespace");
            }

            var img = new TagBuilder("img");

            img.MergeAttribute("src", GetProductCdnSource(src));

            if (!string.IsNullOrWhiteSpace(alt))
            {
                img.MergeAttribute("alt", alt);
            }

            img.TagRenderMode = TagRenderMode.SelfClosing;
            return img;
        }

        public static IHtmlContent ImageBackground(this IHtmlHelper helper, string src)
        {
            var cdnSource = GetCdnSource(src);
            return new HtmlString($"style = \"background-image: url('{cdnSource}')\"");
        }

        public static IHtmlContent Script(this IHtmlHelper helper, string contentPath)
        {
            if (string.IsNullOrWhiteSpace(contentPath))
            {
                throw new ArgumentOutOfRangeException(nameof(contentPath), contentPath, "Must not be null or whitespace");
            }

            var sb = new StringBuilder();
            var paths = Configuration == null ? new[] { contentPath } : Configuration.Scripts[contentPath];

            foreach (var path in paths)
            {
                var script = new TagBuilder("script");

                script.MergeAttribute("type", "text/javascript");
                script.MergeAttribute("src", path);

                using (var writer = new StringWriter())
                {
                    script.WriteTo(writer, HtmlEncoder.Default);
                    sb.AppendLine(writer.ToString());
                }
            }

            return new HtmlString(sb.ToString());
        }

        public static IHtmlContent Styles(this IHtmlHelper helper, string contentPath)
        {
            if (string.IsNullOrWhiteSpace(contentPath))
            {
                throw new ArgumentOutOfRangeException(nameof(contentPath), contentPath, "Must not be null or whitespace");
            }

            var sb = new StringBuilder();
            var paths = Configuration == null ? new[] { contentPath } : Configuration.Styles[contentPath];

            foreach (var path in paths)
            {
                var script = new TagBuilder("link");

                script.MergeAttribute("rel", "stylesheet");
                script.MergeAttribute("href", path);

                using (var writer = new StringWriter())
                {
                    script.WriteTo(writer, HtmlEncoder.Default);
                    sb.AppendLine(writer.ToString());
                }
            }

            return new HtmlString(sb.ToString());
        }

        private static string GetCdnSource(string src)
        {
            if (Configuration == null || string.IsNullOrWhiteSpace(Configuration.Images))
            {
                return src;
            }
            return string.Format("{0}/{1}", Configuration.Images, src);
        }

        private static string GetProductCdnSource(string src)
        {
            if (Configuration == null || string.IsNullOrWhiteSpace(Configuration.ProductImages))
            {
                return GetCdnSource(src);
            }

            var srcUri = new UriBuilder(src);
            if (srcUri.Uri.AbsolutePath != "/")
            {
                var newUri = new UriBuilder(src) { Host = Configuration.ProductImages };
                return newUri.Uri.ToString();
            }

            return GetCdnSource(src);
            
        }
    }
}
