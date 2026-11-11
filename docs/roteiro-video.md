# Roteiro do vídeo (até 2 minutos)

Narração do autor, em português. Tela: a página rodando em `http://localhost:5000`, depois o README.

## 1. Problema (15 s)

"Receitas médicas chegam como foto ou PDF, e alguém acaba digitando tudo à mão: médico, CRM, medicamentos, posologia. É lento e dá erro. Neste projeto eu uso a API da Anthropic para fazer essa extração, com dados fictícios, e meço o quanto dá para confiar nela."

## 2. Demonstração (40 s)

- Clicar no exemplo `rx-21-handwritten.png` com o Claude Opus 5.5: mostrar a receita manuscrita ao lado dos campos extraídos.
- Apontar a **confiança** de cada campo (alta, média) e o destaque "revisar" nos campos que merecem conferência humana, além da situação "Válida" vinda da validação de domínio.
- Trocar para o Claude Haiku 5.5 e repetir a mesma receita: mostrar o rodapé com tokens, custo e latência.
- Clicar em `other-33.png` (um atestado): a API **recusa** com "O documento enviado não é uma receita médica", em vez de inventar campos.

## 3. Como funciona (30 s)

"É uma única chamada ao modelo com **saída estruturada**: um esquema JSON estrito, então a resposta sempre vem no formato certo. Depois, a **validação de domínio em C#** confere CRM, UF, datas e posologia e gera avisos. O acesso à API fica atrás de um **gateway fino**, por isso a suíte de testes roda sem rede e sem credenciais, e o CI do GitHub roda tudo a cada push."

## 4. Avaliação (25 s)

"Montei uma base de 33 documentos com gabarito: PDFs digitados, PNGs digitados, PNGs manuscritos e três documentos que não são receita. Resultado:"

| | Opus 5.5 | Haiku 5.5 |
|---|---|---|
| Precisão por campo | 99,4% | 98,1% |
| Documentos 100% corretos | 93,9% | 81,8% |
| Latência média | 5,5 s | 3,3 s |
| Custo total | US$ 0,9248 | US$ 0,0263 |

"O Haiku custou 2,8% do Opus, com 1,3 ponto percentual a menos de precisão. Para volume alto, vale o Haiku com revisão humana; o Opus fica para os casos difíceis. Um limite honesto: nenhum dos dois modelos reportou confiança baixa, então não consegui medir a calibração."

## 5. Fechamento (10 s)

"O código, o relatório da avaliação e o GIF estão em github.com/Carlosfrayx7/extrator-receitas-ia. Próximos passos: mais receitas manuscritas difíceis na base e forçar o modelo a usar confiança baixa quando estiver em dúvida. Obrigado!"
