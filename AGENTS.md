# AGENTS.md — Guia para agentes de IA

Este arquivo orienta assistentes de IA (Claude, Copilot, Codex, Cursor etc.) que
trabalham neste repositório. Leia tudo antes de mudar qualquer coisa.

## Contexto

- **O que é:** o IEVC Livestream Helper, um app para Windows com pequenos
  "helpers" que ajudam a equipe de transmissão da igreja IEVC. Veja a tabela de
  helpers no [README](README.md).
- **Onde roda:** no computador Windows que roda o OBS (a "mesa de corte"). O
  segundo monitor desse computador é uma TV que fica na sala da mesa de som.
- **Quando é usado:** **ao vivo, durante os cultos.** Um erro aqui atrapalha a
  transmissão em tempo real. Estabilidade vale mais que recurso novo.
- **Quem usa:** voluntários. Muitos **não são técnicos**.

## Quem está pedindo a mudança

A pessoa que conversa com você pode não ser programadora. Então:

1. **Fale em português do Brasil**, de forma simples e sem jargão. Se precisar
   usar um termo técnico, explique numa frase ("*commit* é um ponto salvo no
   histórico do código").
2. **Antes de mudar, confirme o que entendeu** quando o pedido for vago
   ("Você quer que o aviso fique maior na TV, certo? Ou que dure mais tempo?").
   Dê uma recomendação em vez de uma lista de opções.
3. **Depois de mudar, explique o resultado** em linguagem do dia a dia: o que vai
   ser diferente na tela, e o que a pessoa precisa fazer (ex.: "baixe a versão
   nova em Releases").
4. **Nunca peça para a pessoa rodar comandos** que ela não entende sem explicar
   o que cada um faz. Prefira caminhos com cliques (GitHub no navegador,
   Bloco de Notas).
5. Se o pedido puder **quebrar algo que é usado ao vivo** (atalhos, posição do
   aviso, a janela ficar por cima do OBS), avise o risco antes.
6. Muitas mudanças **não precisam de código**: mensagens, atalhos, duração e tela
   da TV ficam no `configuracoes.json` (veja
   [docs/avisos-para-a-tv.md](docs/avisos-para-a-tv.md)). Ofereça esse caminho
   primeiro.

## Regras do projeto (não negociáveis)

- **Todo texto que o usuário vê é em português do Brasil** (janelas, botões,
  mensagens de erro, README, docs, nomes das chaves do `configuracoes.json`).
- **O nome do app é "IEVC Livestream Helper"**, e o executável é
  `IevcLivestreamHelper.exe`. Não invente apelidos ou nomes "fofos" para o app
  nem para os helpers.
- **Um único `.exe` portátil**, sem instalador e sem precisar instalar .NET.
  Não adicione dependências que quebrem isso.
- **Leve.** Não adicione pacotes NuGet sem uma boa razão. Nada de telemetria,
  internet, atualização automática ou serviços em segundo plano.
- **Nada aparece na transmissão.** Janelas sobre a TV devem continuar
  transparentes a cliques, sem roubar o foco do OBS e excluídas de captura de
  tela (veja `OverlayWindow.xaml.cs`).
- **O app nunca pode fechar sozinho durante o culto.** Erros inesperados são
  mostrados e o app continua (veja `App.xaml.cs`).
- **Configuração antiga continua funcionando.** Ao mudar o `AppSettings`, não
  renomeie nem remova chaves existentes do JSON; dê um valor padrão para as novas.
- **Não suba** o `configuracoes.json` de ninguém, `bin/`, `obj/` ou `publish/`
  (já estão no `.gitignore`).

## Investigando erros

- O app grava um log em `%LocalAppData%\IEVC Livestream Helper\logs\log-AAAA-MM-DD.txt`
  (veja `Diagnostics/Log.cs`). Erros saem com o detalhe completo (stack trace).
- Quando alguém relatar um erro, **peça o arquivo de log primeiro**, explicando
  como achá-lo (botão **Abrir registros (log)** na janela do app, ou a seção
  "Deu algum erro?" do README).
- Ao adicionar código novo, registre no log os momentos importantes (`Log.Info`)
  e os problemas (`Log.Warning` / `Log.Error`). Não registre nada pessoal.
- Cuidado com eventos de controles WPF que disparam **durante o
  `InitializeComponent`** (ex.: `ValueChanged` de um `Slider` com `Minimum`).
  Os campos usados por esses eventos precisam estar prontos antes dele.

## Código claro

O código foi escrito para ser lido por quem está aprendendo. Mantenha assim:

- **Nomes em inglês no código**, **comentários em português** explicando o
  *porquê*, não o óbvio.
- Uma responsabilidade por classe e uma pasta por assunto (`Alerts/`,
  `Hotkeys/`, `Monitors/`, `Settings/`, `Views/`, `Native/`).
- Chamadas à API do Windows ficam **só** em `Native/NativeMethods.cs`, cada uma
  com um comentário dizendo o que faz.
- Code-behind simples é preferível a arquiteturas pesadas (MVVM completo,
  injeção de dependência, etc.) enquanto o app for pequeno.
- Siga o `.editorconfig` (4 espaços em C#, 2 em XAML/JSON/YAML).
- `Nullable` está ligado: não deixe avisos de compilação.

## Estrutura

```
src/IevcLivestreamHelper/   O app (C# + WPF, .NET 10)
docs/                       Uma página de ajuda por helper (para usuários)
.github/workflows/          Publicação automática do .exe
```

Detalhes de cada pasta estão no [README](README.md#para-quem-vai-mexer-no-código).

## Compilar e verificar

```bash
dotnet build src/IevcLivestreamHelper -c Release
dotnet publish src/IevcLivestreamHelper -c Release -o publish
```

- Compila em Linux/macOS também (`EnableWindowsTargeting`), mas **só roda no
  Windows**. Se você não consegue executar o app, diga isso claramente à pessoa
  e peça para ela testar no computador do OBS.
- Antes de entregar, o build deve passar **sem erros e sem avisos**.
- O GitHub Actions também **abre o app de verdade** no Windows por alguns
  segundos e falha se o log tiver algum `[ERRO]`. Se esse passo falhar, o log
  aparece na saída do passo.
- Não há testes automatizados de interface. Ao mudar algo visual ou de janela,
  deixe para a pessoa um **roteiro curto de teste**, por exemplo:
  1. Abrir o app e clicar em **Testar**: o aviso aparece na TV?
  2. Com o projetor do OBS em tela cheia, o aviso fica por cima?
  3. Com o OBS em foco, os atalhos (`Ctrl+Alt+1`, `Ctrl+Alt+0`) funcionam?
  4. O aviso some sozinho no tempo configurado?

## Publicação (Releases)

- Todo push na `main` gera o `.exe` e cria uma Release `v1.0.<n>`
  automaticamente (`.github/workflows/publicar.yml`). **Não** é preciso criar
  tags nem Releases à mão.
- Por isso, **não envie direto para a `main`**. Trabalhe numa branch e abra um
  pull request; o workflow compila o PR no Windows para conferir.
- Quem decide fazer o merge é uma pessoa da equipe, nunca o agente.

## Adicionando um helper novo

1. Crie uma pasta para ele em `src/IevcLivestreamHelper/` (ex.: `Timers/`).
2. Se ele precisar de configuração, adicione as chaves novas no `AppSettings`
   (em português no JSON, com valor padrão).
3. Coloque o acesso a ele na janela do operador sem poluir o que já existe;
   o operador precisa achar tudo rápido durante o culto.
4. Escreva a página de ajuda em `docs/<nome-do-helper>.md`, para não técnicos,
   no mesmo estilo de [docs/avisos-para-a-tv.md](docs/avisos-para-a-tv.md).
5. Adicione uma linha na tabela **Helpers** do [README](README.md).

## Mensagens de commit e PR

- Em português, no imperativo e dizendo o efeito para quem usa
  (ex.: "Aumenta o texto do aviso na TV").
- No PR, explique **o que muda na tela** e inclua o roteiro de teste.
