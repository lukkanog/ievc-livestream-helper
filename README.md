# IEVC Livestream Helper

Ferramentas de apoio para a equipe da transmissão (livestream) da IEVC.

O **IEVC Livestream Helper** é um app para Windows que reúne pequenos "helpers"
que facilitam a vida de quem opera a transmissão. Por enquanto existe um helper,
e outros serão adicionados aqui com o tempo.

## Helpers

| Helper | Para que serve |
| ------ | -------------- |
| [Avisos para a TV](docs/avisos-para-a-tv.md) | O operador da mesa de corte (OBS) manda um aviso que aparece bem grande na TV da mesa de som. |

## Como baixar e instalar

Há duas formas. Escolha uma:

### Instalador (recomendado)

👉 **[Baixar o instalador (.msi)](https://github.com/lukkanog/ievc-livestream-helper/releases/latest/download/IevcLivestreamHelper.msi)**

1. Abra o arquivo baixado e siga o assistente (**Avançar → Instalar → Concluir**).
   O Windows vai pedir permissão de administrador.
2. O app fica em **Arquivos de Programas**, com atalhos no **Menu Iniciar** e na
   **Área de Trabalho**, para todos os usuários do computador.
3. **Atualizar:** é só baixar e abrir o instalador da versão nova. Ele substitui
   a antiga sozinho e mantém as suas configurações.
4. **Reparar ou remover:** em *Configurações → Aplicativos → Aplicativos
   instalados* (ou *Painel de Controle → Programas e Recursos*), procure
   **IEVC Livestream Helper**. Abrir o `.msi` de novo também oferece **Reparar**
   e **Remover**.

As configurações da versão instalada ficam em `%AppData%\IEVC Livestream Helper`.

### Portátil (sem instalar)

👉 **[Baixar o .exe portátil](https://github.com/lukkanog/ievc-livestream-helper/releases/latest/download/IevcLivestreamHelper.exe)**

- É um único `.exe`: não precisa instalar nada (o .NET já vai dentro).
- Coloque-o numa pasta só dele (ex.: `Documentos\IEVC Livestream Helper`) e abra.
  As configurações ficam num `configuracoes.json` nessa mesma pasta, então dá
  para levar tudo num pendrive.

### Aviso do Windows

Na primeira vez, o Windows pode mostrar "O Windows protegeu o computador",
porque o app não tem assinatura digital paga. Clique em
**Mais informações → Executar assim mesmo**.

Todas as versões ficam em [Releases](https://github.com/lukkanog/ievc-livestream-helper/releases).

## Deu algum erro?

O app guarda um registro (log) do que acontece, com os detalhes de qualquer erro:

1. Na janela do app, clique em **Abrir registros (log)**. Ou, se a janela nem
   abriu, cole isto na barra de endereço do Explorador de Arquivos:
   `%LocalAppData%\IEVC Livestream Helper\logs`
2. Abra o arquivo do dia (ex.: `log-2026-11-23.txt`).
3. Mande esse arquivo para quem cuida do app, ou cole o conteúdo para a IA
   que estiver ajudando.

Os registros com mais de 14 dias são apagados sozinhos.

## Quer mudar alguma coisa?

- **Mensagens, duração e tela da TV** não precisam de programação:
  veja a página de ajuda do helper (ex.: [Avisos para a TV](docs/avisos-para-a-tv.md)).
- **Quer usar uma IA (Claude, ChatGPT, Copilot…) para mudar o app?** Pode! As
  regras para a IA estão no [AGENTS.md](AGENTS.md). Peça em português, do seu
  jeito, e ela deve explicar o que mudou e como testar.

## Para quem vai mexer no código

Feito em **C# + WPF (.NET 10)**.

```
src/IevcLivestreamHelper/
├── App.xaml(.cs)            Início do app: carrega a configuração e abre as janelas
├── Views/
│   ├── ControlWindow        Janelinha flutuante do operador
│   └── OverlayWindow        Aviso desenhado por cima da TV
├── Alerts/AlertService.cs   Qual aviso está na TV e quanto tempo falta
├── Monitors/                Lista de monitores e escolha da TV
├── Settings/                Leitura e gravação do configuracoes.json
├── Diagnostics/Log.cs       Registro (log) em arquivo texto
├── Startup/SingleInstance.cs  Garante um app aberto por vez (e destrava se precisar)
├── Native/NativeMethods.cs  Chamadas à API do Windows
└── Assets/app.ico           Ícone
installer/                   Instalador .msi (WiX Toolset 5)
docs/                        Uma página de ajuda por helper
```

### Compilar

Precisa do [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# Rodar (no Windows)
dotnet run --project src/IevcLivestreamHelper

# Gerar o .exe portátil em ./publish
dotnet publish src/IevcLivestreamHelper -c Release -o publish

# Gerar o instalador .msi (só no Windows; precisa do passo anterior)
dotnet build installer -c Release
```

### Publicação automática

A cada push na `main`, o GitHub Actions
([`.github/workflows/publicar.yml`](.github/workflows/publicar.yml)) gera o
`IevcLivestreamHelper.exe` e o `IevcLivestreamHelper.msi`, testa os dois no
Windows e cria uma Release nova (`v1.0.<número>`). Em pull
requests ele só compila, para conferir que está tudo certo.

### Adicionando um helper novo

1. Coloque o código numa pasta própria dentro de `src/IevcLivestreamHelper/`.
2. Crie a página de ajuda em `docs/<nome-do-helper>.md`.
3. Adicione uma linha na tabela **Helpers** acima.
