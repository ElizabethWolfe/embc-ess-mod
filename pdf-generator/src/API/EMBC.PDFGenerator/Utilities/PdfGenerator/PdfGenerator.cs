﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace EMBC.PDFGenerator.Utilities.PdfGenerator
{
    public interface IPdfGenerator : IDisposable
    {
        public Task<byte[]> Generate(string source);
    }

    public class PdfGenerator : IPdfGenerator, IDisposable
    {
        private readonly RevisionInfo puppeteerInfo;
        private readonly ILogger<PdfGenerator> logger;
        private readonly Browser browser;
        private bool disposedValue;

        public PdfGenerator(RevisionInfo puppeteerInfo, ILogger<PdfGenerator> logger)
        {
            this.puppeteerInfo = puppeteerInfo;
            this.logger = logger;
            logger.LogInformation("Using Puppeteer from {0}", puppeteerInfo.ExecutablePath);
            browser = Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = puppeteerInfo.ExecutablePath,
                Args = new[] { "--disable-dev-shm-usage", "--no-sandbox", "--no-first-run", "--disable-gpu", "--disable-setuid-sandbox", "--disable-accelerated-2d-canvas", "--no-zygote" },
                Timeout = TimeSpan.FromSeconds(10).Milliseconds,
                LogProcess = true,
                DumpIO = true,
            }).GetAwaiter().GetResult();
            logger.LogInformation("Created Puppeteer browser version {0}", browser.GetVersionAsync().GetAwaiter().GetResult());
        }

        public async Task<byte[]> Generate(string source)
        {
            using var page = await browser.NewPageAsync();
            logger.LogInformation("Created Puppeteer page");
            await page.SetContentAsync(source);
            var content = await page.PdfDataAsync(new PdfOptions { PrintBackground = true });
            logger.LogInformation("Generated pdf with size {0}", content.Length);
            await page.CloseAsync();

            return content;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    browser.Dispose();
                    logger.LogInformation("Browser closed");
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PdfGenerator()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
