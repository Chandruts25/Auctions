using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security;
using System.Net.Http;
using System.IdentityModel.Tokens;
using System.IdentityModel;
using Microsoft.IdentityModel.Protocols;
using System.Threading;

namespace Agape.Auctions.B2C
{
    public class OpenIdConnectCachingSecurityTokenProvider : IIssuerSecurityKeyProvider
    {
        public ConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private string _issuer;
        private IEnumerable<SecurityToken> _tokens;
        private readonly string _metadataEndpoint;

        private readonly ReaderWriterLockSlim _synclock = new ReaderWriterLockSlim();

        public OpenIdConnectCachingSecurityTokenProvider(string metadataEndpoint)
        {
            _metadataEndpoint = metadataEndpoint;
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataEndpoint);

            RetrieveMetadata();
        }

        /// <summary>
        /// Gets the issuer the credentials are for.
        /// </summary>
        /// <value>
        /// The issuer the credentials are for.
        /// </value>
        public string Issuer
        {
            get
            {
                RetrieveMetadata();
                _synclock.EnterReadLock();
                try
                {
                    return _issuer;
                }
                finally
                {
                    _synclock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets all known security tokens.
        /// </summary>
        /// <value>
        /// All known security tokens.
        /// </value>
        public IEnumerable<SecurityToken> SecurityTokens
        {
            get
            {
                RetrieveMetadata();
                _synclock.EnterReadLock();
                try
                {
                    return _tokens;
                }
                finally
                {
                    _synclock.ExitReadLock();
                }
            }
        }

        public IEnumerable<Microsoft.IdentityModel.Tokens.SecurityKey> SecurityKeys => throw new NotImplementedException();

        private void RetrieveMetadata()
        {
            _synclock.EnterWriteLock();
            try
            {
                OpenIdConnectConfiguration config = Task.Run(_configManager.GetConfigurationAsync).Result;
                _issuer = config.Issuer;
                _tokens = config.SigningTokens;
            }
            finally
            {
                _synclock.ExitWriteLock();
            }
        }
    }
}