using FreeCourse.Shared.Dtos;
using FreeCourse.Web.Models;
using FreeCourse.Web.Services.Interfaces;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeCourse.Web.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ClientSettings _clientSettings;
        private readonly ServiceApiSettings _serviceApiSettings;

        public IdentityService(HttpClient client, IHttpContextAccessor httpContextAccessor, IOptions<ClientSettings> clientSettings, IOptions<ServiceApiSettings> serviceApiSettings)
        {
            _httpClient=client;
            _httpContextAccessor=httpContextAccessor;
            _clientSettings=clientSettings.Value;
            _serviceApiSettings=serviceApiSettings.Value;
        }

        public async Task<TokenResponse> GetAccessTokenByRefreshToken()
        {
            // Send a request for discovery endpoint of identity server.
            var disco = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _serviceApiSettings.BaseUri,
                Policy = new DiscoveryPolicy { RequireHttps = false }
            });

            // Check if any error received.
            if (disco.IsError)
            {
                throw disco.Exception;
            }

            var refreshToken = await _httpContextAccessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);

            RefreshTokenRequest refreshTokenRequest = new()
            {
                ClientId = _clientSettings.WebClientForUser.ClientId,
                ClientSecret = _clientSettings.WebClientForUser.ClientSecret,
                RefreshToken = refreshToken,
                Address = disco.TokenEndpoint
            };

            var token = await _httpClient.RequestRefreshTokenAsync(refreshTokenRequest);

            if (token.IsError)
            {
                return null;
            }

            var authenticationTokens = new List<AuthenticationToken>()
            {
                new AuthenticationToken { Name = OpenIdConnectParameterNames.AccessToken, Value = token.AccessToken},
                new AuthenticationToken { Name = OpenIdConnectParameterNames.RefreshToken, Value = token.RefreshToken},
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.ExpiresIn,
                    Value = DateTime.Now.AddSeconds(token.ExpiresIn).ToString("o", CultureInfo.InvariantCulture)
                }
            };

            var authenticationResult = await _httpContextAccessor.HttpContext.AuthenticateAsync();
            var properties = authenticationResult.Properties;

            properties.StoreTokens(authenticationTokens);

            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticationResult.Principal, properties);

            return token;

        }

        public async Task RevokeRefreshToken()
        {
            // Send a request for discovery endpoint of identity server.
            var disco = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _serviceApiSettings.BaseUri,
                Policy = new DiscoveryPolicy { RequireHttps = false }
            });

            // Check if any error received.
            if (disco.IsError)
            {
                throw disco.Exception;
            }

            var refreshToken = await _httpContextAccessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);

            TokenRevocationRequest tokenRevocationRequest = new()
            {
                ClientId = _clientSettings.WebClientForUser.ClientId,
                ClientSecret = _clientSettings.WebClientForUser.ClientSecret,
                Address = disco.RevocationEndpoint,
                Token = refreshToken,
                TokenTypeHint = "refresh_token"
            };

            await _httpClient.RevokeTokenAsync(tokenRevocationRequest);
        }

        public async Task<Response<bool>> SignIn(SignInInput signInInput)
        {
            // Send a request for discovery endpoint of identity server.
            var disco = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _serviceApiSettings.BaseUri,
                Policy = new DiscoveryPolicy { RequireHttps = false }
            });

            // Check if any error received.
            if (disco.IsError)
            {
                throw disco.Exception;
            }

            // Create a password token request.
            var passwordTokenRequest = new PasswordTokenRequest
            {
                ClientId = _clientSettings.WebClientForUser.ClientId,
                ClientSecret = _clientSettings.WebClientForUser.ClientSecret,
                UserName = signInInput.Email,
                Password = signInInput.Password,
                Address = disco.TokenEndpoint
            };

            // Get password token.
            var token = await _httpClient.RequestPasswordTokenAsync(passwordTokenRequest);

            // Check if token received successfully.
            if (token.IsError)
            {
                var responseContent = await token.HttpResponse.Content.ReadAsStringAsync();

                var errorDto = JsonSerializer.Deserialize<ErrorDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return Response<bool>.Fail(errorDto.Errors, 400);
            }

            // Create a user info request
            var userInfoRequest = new UserInfoRequest
            {
                Token = token.AccessToken,
                Address = disco.UserInfoEndpoint
            };

            // Get user info
            var userInfo = await _httpClient.GetUserInfoAsync(userInfoRequest);

            // Check if user info received successfully.
            if (userInfo.IsError)
            {
                throw userInfo.Exception;
            }

            // Create claims identity.
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(userInfo.Claims, CookieAuthenticationDefaults.AuthenticationScheme, "name", "role");

            // Create principal.
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Create authentication properties.
            var authenticationProperties = new AuthenticationProperties();


            // Add token info
            authenticationProperties.StoreTokens(new List<AuthenticationToken>()
            {
                new AuthenticationToken { Name = OpenIdConnectParameterNames.AccessToken, Value = token.AccessToken},
                new AuthenticationToken { Name = OpenIdConnectParameterNames.RefreshToken, Value = token.RefreshToken},
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.ExpiresIn,
                    Value = DateTime.Now.AddSeconds(token.ExpiresIn).ToString("o", CultureInfo.InvariantCulture)
                }
            });

            // Set persistency property.
            authenticationProperties.IsPersistent = signInInput.IsRemember;

            // Finally sign in asynchronously.
            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authenticationProperties);

            // Return success state.
            return Response<bool>.Success(200);
        }
    }
}
