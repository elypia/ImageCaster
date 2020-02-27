﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ImageCasterApi.Models.Request;
using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ImageCasterApi.Controllers
{
    [ApiController]
    [Route("preview")]
    public class PreviewController : ControllerBase
    {       
        private readonly ILogger<PreviewController> _logger;

        public PreviewController(ILogger<PreviewController> logger)
        {
            _logger = logger;
        }
        
        [HttpPost("resize")]
        public ActionResult<string> ResizePreview([FromBody] ResizePreviewModel model)
        {
            FilterType filter = model.Filter;
            string geometry = model.Geometry;
            
            _logger.LogTrace("HTTP request received at preview/resize with filter of {0}, and geometery of {1}.", filter, geometry);
            
            MagickGeometry magickGeometry = new MagickGeometry(geometry);
            _logger.LogInformation("User specified geometry translates to: {0}", magickGeometry);

            using (IMagickImage magickImage = new MagickImage(model.Image.Data))
            {
                magickImage.FilterType = filter;
                magickImage.Resize(magickGeometry);

                string response;
                
                if (model.Image.ContentType != null)
                {
                    response = "data:" + model.Image.ContentType + ";base64," + magickImage.ToBase64();
                }
                else
                {
                    response = magickImage.ToBase64();
                }

                return response;
            }
        }

        [HttpPost("resize-all-filters")]
        public ActionResult<string> ResizeAllFilters([FromBody] FilterPreviewModel model)
        {
            string geometry = model.Geometry;
            
            _logger.LogTrace("HTTP request received at preview/resize with geometery of {0}.", geometry);
            
            MagickGeometry magickGeometry = new MagickGeometry(geometry);
            _logger.LogInformation("User specified geometry translates to: {0}", magickGeometry);

            MontageSettings montageSettings = new MontageSettings()
            {
                BackgroundColor = MagickColors.None,
                Geometry = new MagickGeometry(2, 2, 0, 0),
                TileGeometry = new MagickGeometry(6, 6)
            };

            using (IMagickImage magickImage = new MagickImage(model.Image.Data))
            {
                using (MagickImageCollection collection = new MagickImageCollection())
                {
                    Parallel.ForEach((IEnumerable<FilterType>)typeof(FilterType).GetEnumValues(), (filter) =>
                    {
                        IMagickImage magickImageClone = magickImage.Clone();
                        magickImageClone.FilterType = filter;
                        magickImageClone.Resize(magickGeometry);
                        collection.Add(magickImageClone);
                    });

                    using (IMagickImage montage = collection.Montage(montageSettings))
                    {
                        montage.Format = magickImage.Format;
                    
                        string response;

                        if (model.Image.ContentType != null)
                        {
                            response = "data:" + model.Image.ContentType + ";base64," + montage.ToBase64();
                        }
                        else
                        {
                            response = montage.ToBase64();
                        }

                        return response;
                    }
                }
            }
        }
        
        [HttpPost("modulate")]
        public ActionResult<string> ModulatePreview([FromBody] ModulatePreviewModel model)
        {
            using (IMagickImage magickImage = new MagickImage(model.Image.Data))
            {
                if (model.Mask != null)
                {
                    using (IMagickImage magickImageMask = new MagickImage(model.Mask.Data))
                    {
                        magickImage.SetWriteMask(magickImageMask);
                    }
                }

                magickImage.Modulate(model.Brightness, model.Saturation, model.Hue);
                
                string response;
                
                if (model.Image.ContentType != null)
                {
                    response = "data:" + model.Image.ContentType + ";base64," + magickImage.ToBase64();
                }
                else
                {
                    response = magickImage.ToBase64();
                }

                return response;
            }
        }
    }
}
