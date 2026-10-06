# NFSe Downloader - Sefin Nacional

Aplicação em C# para download de XMLs de NFSe do Portal Nacional do Sefin.

## 📋 Requisitos

- .NET 8 SDK ou superior
- Certificado digital (arquivo `.pfx`)
- Chave de acesso da NFSe (44 ou 50 dígitos)
- Acesso à API do Sefin Nacional

## 🚀 Como usar

### 1. Clonar o repositório

```bash
git clone https://github.com/WAGestao-Fin/NFSeDownloader.git
cd NFSeDownloader
```

### 2. Restaurar dependências

```bash
dotnet restore
```

### 3. Configurar

Abra o `Program.cs` e ajuste:

```csharp
const string BaseUrl = "https://sefin.nfse.gov.br/SefinNacional";
const string CertPath = @"C:\caminho\do\certificado.pfx";  // seu certificado
const string CertPassword = "sua_senha";                   // senha do cert.
const string OutputFolder = @".\NFSe_Downloads";           // pasta de saída

string[] chavasParaTeste = new[]
{
    "26079011244672075000194260000000015026094979232135"  // sua chave aqui
};
```

### 4. Executar

```bash
dotnet run
```

## 📦 Saída

O programa gera:
- ✅ XMLs das NFSe em `./NFSe_Downloads/`
- ✅ Arquivo ZIP compactado com todos os XMLs
- ✅ Logs detalhados no console

## 🔍 Detalhes

- **Endpoint usado**: `https://sefin.nfse.gov.br/SefinNacional/nfse/{chaveAcesso}`
- **Protocolo**: HTTPS (porta 443)
- **Autenticação**: Certificado digital (mTLS)
- **Timeout**: 60 segundos
- **Formato de chave**: 44 ou 50 dígitos numéricos

## ⚠️ Observações

- Este programa aceita chaves de **44 ou 50 dígitos**
- Certifique-se de que o certificado digital corresponde ao CNPJ da NFSe
- Use HTTPS (não HTTP)
- Em caso de erro de conexão, verifique:
  - Se o certificado está correto
  - Se a chave é válida
  - Se você tem acesso à API
  - Se a URL base está correta

## 🛠️ Estrutura do projeto

```
NFSeDownloader/
├── Program.cs                 # Código principal
├── NFSeDownloader.csproj      # Arquivo de projeto
└── README.md                  # Este arquivo
```

## 📝 Desenvolvimento futuro

- [ ] Consulta por CNPJ (lista todas as NFSe)
- [ ] Importação de chaves de arquivo CSV
- [ ] Suporte a múltiplos certificados
- [ ] Agendamento automático
- [ ] Interface gráfica

## 📄 Licença

Uso livre para fins de testes e integração.

## 👨‍💻 Autor

WAGestao-Fin
