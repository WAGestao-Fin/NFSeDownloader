# Configuração de exemplo

Este arquivo mostra como configurar o programa.

## Exemplo 1: Uma chave

```csharp
const string BaseUrl = "https://sefin.nfse.gov.br/SefinNacional";
const string CertPath = @"C:\Users\SeuUsuario\Documents\certificado.pfx";
const string CertPassword = "1234567890";
const string OutputFolder = @"C:\NFSe";

string[] chavasParaTeste = new[]
{
    "26079011244672075000194260000000015026094979232135"
};
```

## Exemplo 2: Múltiplas chaves

```csharp
string[] chavasParaTeste = new[]
{
    "26079011244672075000194260000000015026094979232135",
    "26079011244672075000194260000000015026094979232136",
    "26079011244672075000194260000000015026094979232137"
};
```

## Exemplo 3: Digitar chaves em tempo real

Deixe o array vazio:

```csharp
string[] chavasParaTeste = new string[0];
```

O programa pedirá para você digitar as chaves.

## Endpoints

- **Base**: `https://sefin.nfse.gov.br/SefinNacional`
- **Download por chave**: `/nfse/{chaveAcesso}`
- **Protocolo**: HTTPS (porta 443)
- **Timeout**: 60 segundos

## Validação de Chave

Formato aceito:
- 44 dígitos: `00000000000000000000000000000000000000000000`
- 50 dígitos: `26079011244672075000194260000000015026094979232135`

Apenas números são aceitos.

## Arquivo de Certificado

O certificado deve estar em formato `.pfx` (PKCS#12).

Caminho recomendado:
- Windows: `C:\Users\SeuUsuario\AppData\Local\Certificates\`
- Linux/Mac: `~/.certificates/`

## Pasta de Saída

Os arquivos serão salvos em:
```
.\NFSe_Downloads\
├── NFSe_26079011244672075000194260000000015026094979232135.xml
└── NFSe_20261006_132735.zip
```

## Resolução de Problemas

### Erro: "Certificado não encontrado"
- Verifique o caminho em `CertPath`
- Certifique-se de que o arquivo existe

### Erro: "Senha do certificado inválida"
- Verifique a senha em `CertPassword`
- Tente usar aspas duplas: `"minha@senha!"`

### Erro: "Erro de conexão"
- Verifique se está usando HTTPS (não HTTP)
- Verifique a URL base
- Tente aumentar o timeout (60 segundos)

### Erro: "Resposta vazia"
- A chave pode estar inválida
- Ou o certificado não corresponde ao CNPJ

## Suporte

Para mais informações, consulte a documentação oficial do Sefin Nacional:
https://sefin.nfse.gov.br/SefinNacional/docs/index
