using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            const string BaseUrl = "http://sefin.nfse.gov.br/SefinNacional";
            const string CertPath = @"C:\certificados\certificado.pfx";
            const string CertPassword = "sua_senha";
            const string OutputFolder = @".\NFSe_Downloads";
            
            // Exemplo: coloque aqui as chaves que você quer testar
            // Aceita tanto chaves de 44 dígitos quanto de 50 dígitos
            string[] chavasParaTeste = new[]
            {
                "26079011244672075000194260000000015026094979232135" // 50 dígitos
            };
            // ===== FIM CONFIGURAÇÕES =====

            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            Console.WriteLine($"📁 Pasta de saída: {Path.GetFullPath(OutputFolder)}\n");

            try
            {
                var downloader = new SefinDownloader(BaseUrl, CertPath, CertPassword);

                // Se não houver chaves configuradas, pede para digitar
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
            
            // Aceita tanto 44 dígitos (chave NFe/NFSe padrão) quanto 50 dígitos (formato estendido)
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
                var cert = new X509Certificate2(
                    certPath,
                    certPassword,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

                var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (message, cert2, chain, errors) => true
                };

                handler.ClientCertificates.Add(cert);

                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                _httpClient.DefaultRequestHeaders.Add("User-Agent", "NFSeDownloader/1.0");
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/xml"));
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("text/xml"));
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                Console.WriteLine("✅ Certificado digital carregado com sucesso\n");
            }
            catch (FileNotFoundException)
            {
                throw new Exception($"Certificado não encontrado: {certPath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar certificado: {ex.Message}", ex);
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
                using var response = await _httpClient.GetAsync(url);

                var body = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"   📡 Status: {(int)response.StatusCode} - {response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    if (!string.IsNullOrWhiteSpace(body) && body.Length < 200)
                        Console.WriteLine($"   Resposta: {body}");
                    return string.Empty;
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    Console.WriteLine($"   ⚠️  Resposta vazia");
                    return string.Empty;
                }

                // Validar se é XML
                if (body.TrimStart().StartsWith("<"))
                {
                    // Tentar fazer parse para garantir que é XML válido
                    try
                    {
                        var doc = new XmlDocument();
                        doc.LoadXml(body);
                        Console.WriteLine($"   ✅ XML válido");
                        return body;
                    }
                    catch (XmlException ex)
                    {
                        Console.WriteLine($"   ⚠️  XML inválido: {ex.Message}");
                        return body; // Ainda assim retorna para o usuário verificar
                    }
                }

                // Se for JSON, tenta extrair XML
                if (body.TrimStart().StartsWith("{"))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;

                        // Procura por campo que contenha XML
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
                Console.WriteLine($"   ⚠️  Formato desconhecido (não é XML nem JSON)");
                return body;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"   ❌ Erro de conexão: {ex.Message}");
                return string.Empty;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"   ❌ Timeout na requisição");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Erro inesperado: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
