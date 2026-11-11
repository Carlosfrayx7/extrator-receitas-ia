# Extrator de receitas médicas com IA - especificação

Data: 2026-11-11 · Status: aprovado no brainstorming, aguardando revisão do texto

## 1. Objetivo

Projeto de portfólio de engenharia de IA: uma API em .NET que recebe a imagem ou o PDF de uma receita médica **fictícia** e devolve os dados em JSON validado, com a confiança de cada campo e avisos de validação de domínio. O projeto mede a própria qualidade com uma base de teste com gabarito e compara dois modelos (Claude Opus 5.5 e Claude Haiku 5.5) em precisão, custo e latência.

O que o projeto demonstra:
- saída estruturada com esquema JSON estrito (structured outputs da API da Anthropic);
- entrada multimodal (imagem e PDF);
- validação de domínio sobre a saída do modelo;
- avaliação com métricas, calibração de confiança e comparação de modelos;
- engenharia de produção: testes sem chamar o modelo, CI, tratamento de erros e controle de custo.

**Fora do escopo:** transcrição em duas etapas, OCR próprio, cache de prompt (o prefixo comum é curto demais para ser armazenado), autenticação de usuários, persistência de extrações e qualquer uso com dados reais de pacientes.

## 2. Decisões

| Tema | Decisão |
|---|---|
| Linguagem | C# / .NET 10 (LTS) |
| SDK | pacote oficial `Anthropic` (NuGet) |
| Abordagem | uma chamada com saída estruturada (`OutputConfig.Format`) + validação de domínio em C# |
| Modelos | `claude-opus-5-5` (padrão) e `claude-haiku-5-5`, configuráveis; esforço padrão `medium` |
| Entrada | PNG, JPEG e PDF; até 10 MB e 5 páginas |
| Interface | API ASP.NET Core minimal + página estática em HTML/CSS/JS puro |
| Chave | somente `ANTHROPIC_API_KEY`; nunca em arquivo, log ou repositório |
| Idioma | código e identificadores em inglês; README em português com resumo em inglês |
| Publicação | repositório público `extrator-receitas-ia` no GitHub do autor, após revisão |

## 3. Estrutura do repositório

```
extrator-receitas-ia/
├─ src/
│  ├─ PrescriptionExtractor.Core/       domínio, extrator, esquema e validação (sem dependência de web)
│  ├─ PrescriptionExtractor.Api/        minimal API + wwwroot/index.html
│  └─ PrescriptionExtractor.Eval/       console de avaliação em lote
├─ tools/DatasetGenerator/              gera a base fictícia + gabaritos
├─ dataset/                             documentos gerados e gabaritos (versionados)
├─ tests/
│  ├─ PrescriptionExtractor.Core.Tests/
│  └─ PrescriptionExtractor.Api.Tests/
├─ docs/                                demo.gif, eval-report.md, roteiro-video.md
├─ .github/workflows/ci.yml
└─ README.md
```

Unidades principais do Core:
- `Prescription` e `ExtractedField<T>` (valor + confiança);
- `IPrescriptionExtractor.ExtractAsync(Stream, string contentType, ExtractionOptions, CancellationToken)` devolvendo `ExtractionResult` (receita, situação, avisos, modelo, tokens, custo estimado, latência);
- `ClaudePrescriptionExtractor`: monta a requisição (bloco de imagem ou `DocumentBlockParam` com `Base64PdfSource`, prompt de sistema, esquema) e interpreta a resposta; depende de uma abstração fina sobre o cliente da API para permitir dublê;
- `PrescriptionSchema`: o esquema JSON da seção 4;
- `PrescriptionValidator`: as regras da seção 5;
- `ModelPricing`: tabela de preço por modelo para o custo estimado.

## 4. Esquema enviado ao modelo

JSON estrito: todos os campos obrigatórios, `additionalProperties: false`; campo não lido vem como `null`.

```
Prescription
├─ isPrescription      bool
├─ prescriptionType    "simples" | "controle_especial" | "antimicrobiano" | "desconhecido"
├─ prescriber { name, crmNumber, crmState }    cada um Field<string>
├─ patientName         Field<string>
├─ issueDate           Field<string>   (yyyy-MM-dd)
└─ items[] { medication, strength, form, dosage, duration, quantity }   cada um Field<string>

Field<T> = { value: T | null, confidence: "high" | "medium" | "low" }
```

Prompt de sistema: extração literal, sem completar nem corrigir grafia; ilegível vira `null` com confiança `low`; `isPrescription = false` para documentos que não são receita.

## 5. Validação de domínio

| Regra | Gravidade |
|---|---|
| `isPrescription = false` | erro (API responde 422) |
| nenhum item | erro |
| item sem medicamento ou sem posologia | erro |
| CRM fora de 4 a 7 dígitos, ou UF fora das 27 | aviso |
| data inválida, futura ou com mais de 30 dias | aviso |
| concentração sem unidade reconhecida (mg, g, mcg, mL, UI, %) | aviso |
| antimicrobiano sem duração | aviso |
| campo com confiança `low` | aviso "revisar manualmente" |

Situação: `invalid` (há erro), `needs_review` (só avisos), `valid` (nada).

## 6. API e página

- `POST /extractions` (multipart: `file`, `model` opcional `opus`|`haiku`) → 200 com `ExtractionResult`.
- `GET /health` → indica se a chave está configurada, sem expor o valor.
- Erros: 400 tipo/tamanho inválido; 422 não é receita ou recusa do modelo (`stop_reason = refusal`); 503 limite de requisições (rate limit); 502 outros erros da API. Mensagem amigável; log sem conteúdo do documento.
- Sem chave: a API sobe e a página mostra como configurar.
- Página: arrastar e soltar, seletor de modelo, prévia, campos com etiqueta de confiança, destaque "revisar", situação colorida, avisos, rodapé com modelo/tokens/custo/tempo e três exemplos clicáveis.

## 7. Base de teste

Gerador em C# (QuestPDF para PDF, SkiaSharp para imagem), semente fixa, 33 documentos:
- 10 PDF digitados, 10 PNG digitados, 10 PNG "manuscritos" (fonte Caveat, licença OFL incluída, com rotação, desfoque e ruído), 3 documentos que não são receita (nota fiscal, cardápio, atestado);
- dados inventados (nomes, CRM com formato válido), medicamentos comuns e públicos, 1 a 3 itens, tipos variados e algumas datas antigas;
- cada documento com `<nome>.expected.json`.

## 8. Avaliação

`dotnet run --project src/PrescriptionExtractor.Eval -- --models claude-opus-5-5,claude-haiku-5-5`

- Usa o mesmo extrator da API; 4 requisições em paralelo.
- Normaliza antes de comparar: caixa, acentos, espaços e unidades ("500mg" = "500 mg"); itens pareados pelo nome do medicamento.
- Métricas por modelo: precisão por campo e geral, por tipo de documento; documentos 100% corretos; acerto na recusa; JSON válido; erros da API; calibração da confiança (acerto em `high` x `low`); latência média e p95; tokens e custo total.
- Antes de rodar, mostra a estimativa de custo (cerca de US$ 0,70 para os dois modelos) e pede confirmação.
- Saída: `docs/eval-report.md` (tabela comparativa, erros mais comuns, conclusão de custo x precisão) e resultados brutos em JSON.

## 9. Testes e CI

- xUnit + FluentAssertions, sem chamar a API: validador (todas as regras e bordas), mapeamento do JSON (incompleto, `null`), extrator com dublê (montagem de imagem/PDF, esquema, recusa, `max_tokens`), API com `WebApplicationFactory` (200/400/422/503 e health), normalização da avaliação.
- Um teste de integração com a API real, executado só com a chave definida; o CI o pula.
- GitHub Actions em push e PR: restore, build, testes e execução do gerador. Sem segredos. Badge no README.

## 10. Demonstração e documentação

- `docs/demo.gif` (~20 s, gravado com Playwright): receita manuscrita → campos e confiança → troca para Haiku → comparação de custo.
- `docs/roteiro-video.md`: roteiro de 1 a 2 minutos para o vídeo narrado pelo autor.
- README: problema, GIF, diagrama Mermaid, como rodar em 3 comandos, tabela da avaliação, decisões técnicas e aviso de responsabilidade (demonstração com dados fictícios; não é dispositivo médico).

## 11. Critérios de pronto

- Build e testes passam localmente e no CI.
- Avaliação executada com os dois modelos e relatório publicado no repositório.
- GIF e README completos; nenhum segredo nem dado real no histórico do git.
- Autor revisou o conteúdo antes da publicação.
