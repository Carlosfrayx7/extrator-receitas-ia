# Avaliação: Claude Opus 5.5 × Claude Haiku 5.5

Executada em 11/11/2026 sobre 33 documentos fictícios (10 PDF digitados, 10 PNG digitados, 10 PNG manuscritos, 3 que não são receita). Esforço `medium`.

| Métrica | Opus 5.5 | Haiku 5.5 |
|---|---|---|
| Precisão por campo | 99,4% | 98,1% |
|   · PDF digitado | 100,0% | 97,1% |
|   · PNG digitado | 98,2% | 100,0% |
|   · PNG manuscrito | 100,0% | 97,3% |
| Documentos 100% corretos | 93,9% | 81,8% |
| Reconhece o que não é receita | 100,0% | 100,0% |
| JSON válido | 100,0% | 100,0% |
| Erros da API | 0 | 0 |
| Acerto quando confiança alta | 99,4% (n=497) | 98,0% (n=495) |
| Acerto quando confiança baixa | n/d | n/d |
| Latência média | 5,5 s | 3,3 s |
| Latência p95 | 8,1 s | 5,9 s |
| Tokens (entrada / saída) | 159.958 / 14.248 | 159.958 / 20.700 |
| Custo total | US$ 0,9248 | US$ 0,0263 |

## Campos que mais erraram

**Opus 5.5**
- `item[].duration`: 2 erro(s)
- `prescriber.name`: 1 erro(s)

**Haiku 5.5**
- `item[].dosage`: 5 erro(s)
- `item[].duration`: 5 erro(s)

## Conclusão

O Haiku 5.5 custou 2,8% do Opus 5.5, com 1,3 pontos percentuais de diferença na precisão por campo.
