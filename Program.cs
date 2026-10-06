using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace NFSeDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            
            Console.WriteLine("╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║       Download de NFSe - Sefin Nacional               ║");
            Console.WriteLine("║            Programa de Testes Local                   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

            // ===== CONFIGURAÇÕES =====
            // IMPORTANTE: Use HTTPS em vez de HTTP
            const string BaseUrl = "https://sefin.nfse.gov.br/SefinNacional";
            const string CertPath = @"C:\certificados\certificado.pfx";
            const string CertPassword = "sua_senha";
            const string OutputFolder = @".\NFSe_Downloads";
            
            // Chave para teste
            string[] chavasParaTeste = new[]
            {
                "26079011244672075000194260000000015026094979232135"
            };
            // ===== FIM CONFIGURAÇÕES =====

            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            Console.WriteLine($"📁 Pasta de saída: {Path.GetFullPath(OutputFolder)}\n");
            Console.WriteLine($"🔐 Usando HTTPS (porta 443)\n");

            try
            {
                var downloader = new SefinDownloader(BaseUrl, CertPath, CertPassword);

                if (chavasParaTeste.Length == 0 || string.IsNullOrWhiteSpace(chavasParaTeste[0]))
                {
                    chavasParaTeste = await SolicitarChavasDoUsuario();
                }

                if (chavasParaTeste.Length == 0)
                {
                    Console.WriteLine("❌ Nenhuma chave foi informada. Encerrando.");
                    return;
                }

                var arquivosBaixados = new List<string>();

                Console.WriteLine($"📥 Iniciando download de {chavasParaTeste.Length} chave(s)...\n");
                Console.WriteLine("═══════════════════════════════════════════════════════\n");

                int sucesso = 0;
                int falha = 0;

                foreach (var (chave, index) in chavasParaTeste.Enumerate())
                {
                    Console.WriteLine($"[{index + 1:D2}/{chavasParaTeste.Length}] Testando chave: {chave}");

                    if (!ValidarChaveAcesso(chave))
                    {
                        Console.WriteLine($"   ❌ Formato inválido (deve ter 44 ou 50 dígitos)\n");
                        falha++;
                        continue;
                    }

                    try
                    {
                        var xml = await downloader.DownloadXmlPorChaveAsync(chave);

                        if (!string.IsNullOrWhiteSpace(xml))
                        {
                            var nomeArquivo = $"NFSe_{chave}.xml";
                            var caminhoCompleto = Path.Combine(OutputFolder, nomeArquivo);
                            
                            await File.WriteAllTextAsync(caminhoCompleto, xml, Encoding.UTF8);
                            arquivosBaixados.Add(caminhoCompleto);

                            Console.WriteLine($"   ✅ XML salvo com sucesso");
                            Console.WriteLine($"   📄 Arquivo: {nomeArquivo}");
                            Console.WriteLine($"   📊 Tamanho: {new FileInfo(caminhoCompleto).Length} bytes\n");
                            sucesso++;
                        }
                        else
                        {
                            Console.WriteLine($"   ❌ Resposta vazia\n");
                            falha++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ Erro: {ex.Message}\n");
                        falha++;
                    }
                }

                Console.WriteLine("═══════════════════════════════════════════════════════\n");
                Console.WriteLine($"📊 RESUMO FINAL:");
                Console.WriteLine($"   ✅ Sucesso: {sucesso}");
                Console.WriteLine($"   ❌ Falha:   {falha}\n");

                if (arquivosBaixados.Count > 0)
                {
                    Console.WriteLine($"📁 Arquivos baixados em: {Path.GetFullPath(OutputFolder)}\n");
                    
                    // Criar ZIP
                    await CriarZipComArquivos(OutputFolder, arquivosBaixados);
                }
                else
                {
                    Console.WriteLine("⚠️  Nenhum arquivo foi baixado.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERRO FATAL: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"   Detalhes: {ex.InnerException.Message}");
                Environment.Exit(1);
            }

            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        static async Task<string[]> SolicitarChavasDoUsuario()
        {
            Console.WriteLine("📝 Nenhuma chave configurada. Digite as chaves para testar:\n");
            Console.WriteLine("(Cada chave deve ter 44 ou 50 dígitos numéricos)");
            Console.WriteLine("(Digite 'sair' quando terminar)\n");

            var chaves = new List<string>();

            while (true)
            {
                Console.Write($"Chave {chaves.Count + 1}: ");
                var entrada = Console.ReadLine()?.Trim();

                if (entrada?.ToLower() == "sair")
                    break;

                if (string.IsNullOrWhiteSpace(entrada))
                    continue;

                chaves.Add(entrada);
            }

            return chaves.ToArray();
        }

        static bool ValidarChaveAcesso(string chave)
        {
            if (string.IsNullOrWhiteSpace(chave))
                return false;

            var apenasDigitos = new string(System.Linq.Enumerable.Where(chave, char.IsDigit).ToArray());
            
            return apenasDigitos.Length == 44 || apenasDigitos.Length == 50;
        }

        static async Task CriarZipComArquivos(string pastaBase, List<string> arquivos)
        {
            try
            {
                var nomeZip = $"NFSe_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                var caminhoZip = Path.Combine(pastaBase, nomeZip);

                Console.WriteLine($"📦 Criando arquivo ZIP: {nomeZip}");

                using (var zip = ZipFile.Open(caminhoZip, ZipArchiveMode.Create))
                {
                    foreach (var arquivo in arquivos)
                    {
                        if (File.Exists(arquivo))
                        {
                            var nomeNoZip = Path.GetFileName(arquivo);
                            zip.CreateEntryFromFile(arquivo, nomeNoZip);
                        }
                    }
                }

                var tamanhoZip = new FileInfo(caminhoZip).Length;
                Console.WriteLine($"   ✅ ZIP criado com sucesso!");
                Console.WriteLine($"   📦 Arquivo: {nomeZip}");
                Console.WriteLine($"   📊 Tamanho: {tamanhoZip} bytes ({tamanhoZip / 1024.0:F2} KB)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n⚠️  Erro ao criar ZIP: {ex.Message}");
            }
        }
    }

    // Extension para facilitar enumerate com index
    public static class EnumerableExtensions
    {
        public static IEnumerable<(T item, int index)> Enumerate<T>(this T[] items)
        {
            for (int i = 0; i < items.Length; i++)
                yield return (items[i], i);
        }
    }

    public class SefinDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public SefinDownloader(string baseUrl, string certPath, string certPassword)
        {
            _baseUrl = baseUrl.TrimEnd('/');

            try
            {
                Console.WriteLine("⚙️  Configurando cliente HTTPS com certificado digital...\n");

                X509Certificate2 cert = null;

                // Tentar carregar certificado
                if (File.Exists(certPath))
                {
                    cert = new X509Certificate2(
                        certPath,
                        certPassword,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

                    Console.WriteLine($"✅ Certificado carregado: {cert.Subject}");
                    Console.WriteLine($"   Válido de: {cert.NotBefore:dd/MM/yyyy}");
                    Console.WriteLine($"   Válido até: {cert.NotAfter:dd/MM/yyyy}\n");
                }
                else
                {
                    Console.WriteLine($"⚠️  AVISO: Certificado não encontrado em {certPath}");
                    Console.WriteLine("   Continuando sem certificado (pode falhar se a API exigir)...\n");
                }

                // Configurar handler HTTP
                var handler = new HttpClientHandler();

                if (cert != null)
                {
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    handler.ClientCertificates.Add(cert);
                }

                // Desabilitar validação de certificado SSL (apenas para testes)
                // ⚠️ NUNCA use isso em produção!
                handler.ServerCertificateCustomValidationCallback = (message, cert2, chain, errors) =>
                {
                    if (errors == System.Net.Security.SslPolicyErrors.None)
                        return true;

                    Console.WriteLine($"⚠️  Aviso SSL: {errors}");
                    return true; // Aceitar mesmo assim (apenas para testes)
                };

                // Configurar timeout e outras opções
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };

                _httpClient.DefaultRequestHeaders.Add("User-Agent", "NFSeDownloader/1.0");
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/xml"));
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("text/xml"));
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("*/*"));

                Console.WriteLine("✅ Cliente HTTP configurado com sucesso\n");
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao configurar cliente: {ex.Message}", ex);
            }
        }

        public async Task<string> DownloadXmlPorChaveAsync(string chaveAcesso)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso))
                throw new ArgumentException("Chave de acesso obrigatória.");

            var url = $"{_baseUrl}/nfse/{chaveAcesso}";

            Console.WriteLine($"   🔗 URL: {url}");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept-Language", "pt-BR,pt;q=0.9,en;q=0.8");

                Console.WriteLine($"   ⏳ Aguardando resposta...");

                using var response = await _httpClient.SendAsync(request);

                var body = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"   📡 Status: {(int)response.StatusCode} - {response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        var preview = body.Length > 500 ? body.Substring(0, 500) + "..." : body;
                        Console.WriteLine($"   Resposta: {preview}");
                    }
                    return string.Empty;
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    Console.WriteLine($"   ⚠️  Resposta vazia");
                    return string.Empty;
                }

                Console.WriteLine($"   📦 Tamanho da resposta: {body.Length} bytes");

                // Validar se é XML
                if (body.TrimStart().StartsWith("<"))
                {
                    try
                    {
                        var doc = new XmlDocument();
                        doc.LoadXml(body);
                        Console.WriteLine($"   ✅ XML válido e bem formado");
                        return body;
                    }
                    catch (XmlException ex)
                    {
                        Console.WriteLine($"   ⚠️  XML não bem formado: {ex.Message}");
                        return body;
                    }
                }

                // Se for JSON, tenta extrair XML
                if (body.TrimStart().StartsWith("{"))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;

                        foreach (var prop in root.EnumerateObject())
                        {
                            var valor = prop.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(valor) && valor.TrimStart().StartsWith("<"))
                            {
                                Console.WriteLine($"   ✅ XML extraído do JSON (campo: {prop.Name})");
                                return valor;
                            }
                        }

                        Console.WriteLine($"   ⚠️  JSON recebido mas nenhum campo contém XML");
                        return body;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ⚠️  Erro ao processar JSON: {ex.Message}");
                        return body;
                    }
                }

                // Retorno desconhecido
                Console.WriteLine($"   ⚠️  Formato desconhecido");
                return body;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"   ❌ Erro de conexão HTTP:");
                Console.WriteLine($"      {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"      Causa: {ex.InnerException.Message}");
                return string.Empty;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"   ❌ Timeout na requisição (60 segundos)");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Erro inesperado: {ex.GetType().Name}");
                Console.WriteLine($"      {ex.Message}");
                return string.Empty;
            }
        }
    }
}
