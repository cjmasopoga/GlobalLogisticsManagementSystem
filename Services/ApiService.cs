using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Global_Logistics_Management_System.Models.Api;
using Microsoft.AspNetCore.Http;

namespace Global_Logistics_Management_System.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _ctx;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public ApiService(HttpClient http, IHttpContextAccessor ctx)
        {
            _http = http;
            _ctx = ctx;
        }

        private void AttachToken()
        {
            var token = _ctx.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }

        // ── Auth ─────────────────────────────────────────────────────────────

        public async Task<TokenDto?> LoginAsync(string username, string password)
        {
            var resp = await _http.PostAsync("api/auth/login",
                new StringContent(
                    JsonSerializer.Serialize(new LoginDto(username, password)),
                    Encoding.UTF8, "application/json"));

            if (!resp.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<TokenDto>(
                await resp.Content.ReadAsStringAsync(), _json);
        }

        // ── Clients ─────────────────────────────────────────────────────────

        public async Task<List<ClientDto>> GetClientsAsync()
        {
            AttachToken();
            var json = await _http.GetStringAsync("api/clients");
            return JsonSerializer.Deserialize<List<ClientDto>>(json, _json)!;
        }

        public async Task<ClientDto?> GetClientAsync(int id)
        {
            AttachToken();
            var resp = await _http.GetAsync($"api/clients/{id}");
            if (!resp.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<ClientDto>(await resp.Content.ReadAsStringAsync(), _json);
        }

        public async Task<bool> CreateClientAsync(CreateClientDto dto)
        {
            AttachToken();
            var resp = await _http.PostAsync("api/clients",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateClientAsync(int id, CreateClientDto dto)
        {
            AttachToken();
            var resp = await _http.PutAsync($"api/clients/{id}",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            AttachToken();
            var resp = await _http.DeleteAsync($"api/clients/{id}");
            return resp.IsSuccessStatusCode;
        }

        // ── Contracts ────────────────────────────────────────────────────────

        public async Task<List<ContractDto>> GetContractsAsync(string? status = null,
            DateTime? from = null, DateTime? to = null)
        {
            AttachToken();
            var query = new List<string>();
            if (!string.IsNullOrEmpty(status)) query.Add($"status={status}");
            if (from.HasValue) query.Add($"startDateFrom={from.Value:yyyy-MM-dd}");
            if (to.HasValue) query.Add($"startDateTo={to.Value:yyyy-MM-dd}");
            var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
            var json = await _http.GetStringAsync($"api/contracts{qs}");
            return JsonSerializer.Deserialize<List<ContractDto>>(json, _json)!;
        }

        public async Task<ContractDto?> GetContractAsync(int id)
        {
            AttachToken();
            var resp = await _http.GetAsync($"api/contracts/{id}");
            if (!resp.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<ContractDto>(await resp.Content.ReadAsStringAsync(), _json);
        }

        public async Task<(bool Success, int Id)> CreateContractAsync(CreateContractDto dto, IFormFile? pdf)
        {
            AttachToken();
            var resp = await _http.PostAsync("api/contracts",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
            if (!resp.IsSuccessStatusCode) return (false, 0);

            var created = JsonSerializer.Deserialize<ContractDto>(
                await resp.Content.ReadAsStringAsync(), _json)!;

            if (pdf != null && pdf.Length > 0)
                await UploadAgreementAsync(created.Id, pdf);

            return (true, created.Id);
        }

        public async Task<bool> UpdateContractAsync(int id, CreateContractDto dto)
        {
            AttachToken();
            var resp = await _http.PutAsync($"api/contracts/{id}",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> PatchContractStatusAsync(int id, string status)
        {
            AttachToken();
            var dto = new PatchContractStatusDto(status);
            var resp = await _http.PatchAsync($"api/contracts/{id}/status",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteContractAsync(int id)
        {
            AttachToken();
            var resp = await _http.DeleteAsync($"api/contracts/{id}");
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> UploadAgreementAsync(int contractId, IFormFile pdf)
        {
            AttachToken();
            using var form = new MultipartFormDataContent();
            using var ms = new MemoryStream();
            await pdf.CopyToAsync(ms);
            form.Add(new ByteArrayContent(ms.ToArray()), "file", pdf.FileName);
            var resp = await _http.PostAsync($"api/contracts/{contractId}/upload", form);
            return resp.IsSuccessStatusCode;
        }

        public async Task<(byte[] Bytes, string FileName)?> DownloadAgreementAsync(int contractId)
        {
            AttachToken();
            var resp = await _http.GetAsync($"api/contracts/{contractId}/download");
            if (!resp.IsSuccessStatusCode) return null;
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            var name = resp.Content.Headers.ContentDisposition?.FileName ?? "agreement.pdf";
            return (bytes, name.Trim('"'));
        }

        // ── Service Requests ─────────────────────────────────────────────────

        public async Task<List<ServiceRequestDto>> GetServiceRequestsAsync()
        {
            AttachToken();
            var json = await _http.GetStringAsync("api/servicerequests");
            return JsonSerializer.Deserialize<List<ServiceRequestDto>>(json, _json)!;
        }

        public async Task<ServiceRequestDto?> GetServiceRequestAsync(int id)
        {
            AttachToken();
            var resp = await _http.GetAsync($"api/servicerequests/{id}");
            if (!resp.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<ServiceRequestDto>(await resp.Content.ReadAsStringAsync(), _json);
        }

        public async Task<(bool Success, string? Error)> CreateServiceRequestAsync(CreateServiceRequestDto dto)
        {
            AttachToken();
            var resp = await _http.PostAsync("api/servicerequests",
                new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
            if (resp.IsSuccessStatusCode) return (true, null);
            var body = await resp.Content.ReadAsStringAsync();
            return (false, body);
        }

        public async Task<bool> PatchServiceRequestStatusAsync(int id, string status)
        {
            AttachToken();
            var resp = await _http.PatchAsync($"api/servicerequests/{id}/status",
                new StringContent(JsonSerializer.Serialize(new { Status = status }), Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteServiceRequestAsync(int id)
        {
            AttachToken();
            var resp = await _http.DeleteAsync($"api/servicerequests/{id}");
            return resp.IsSuccessStatusCode;
        }

        public async Task<decimal> GetExchangeRateAsync()
        {
            AttachToken();
            var json = await _http.GetStringAsync("api/servicerequests/exchange-rate");
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("rate").GetDecimal();
        }
    }
}
