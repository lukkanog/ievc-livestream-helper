# 🔔 Sininho da Live — IEVC

Um "sininho" para a transmissão da IEVC: o operador da **mesa de corte** (OBS)
manda um aviso, e ele aparece **bem grande na TV da mesa de som**.

- Só visual (sem som), fofinho mas impossível de não ver: cartão arredondado
  amarelo/coral, sininho balançando, pulsação suave e uma barrinha mostrando
  quanto tempo falta.
- O aviso some sozinho depois de **30 segundos** (dá para mudar) ou quando o
  operador clica em **Limpar**.
- Aparece **por cima do projetor do OBS**, não recebe cliques, não rouba o foco
  e **não vai para a transmissão**.
- Um único `.exe` portátil: não precisa instalar nada.

## Como baixar

Baixe o arquivo **`SininhoDaLive.exe`** da última versão:

👉 **[Baixar a última versão](https://github.com/lukkanog/ievc-livestream-helper/releases/latest/download/SininhoDaLive.exe)**

(ou veja todas em [Releases](https://github.com/lukkanog/ievc-livestream-helper/releases)).

Coloque o `.exe` numa pasta só dele (ex.: `Documentos\Sininho`) e abra.
Na primeira vez, o Windows pode mostrar "O Windows protegeu o computador":
clique em **Mais informações → Executar assim mesmo**.

> O app cria um arquivo `configuracoes.json` na mesma pasta. Por isso não o coloque
> em `Arquivos de Programas`, onde o Windows não deixa gravar.

## Como usar

1. Abra o **SininhoDaLive.exe** no computador do OBS.
2. Em **Tela da TV**, escolha o monitor que é a TV da mesa de som e clique em
   **Testar** para conferir.
3. Pronto! Para mandar um aviso:
   - clique em uma **mensagem rápida**, ou
   - digite uma **mensagem personalizada** e aperte **Enter**, ou
   - use os **atalhos de teclado** (funcionam mesmo com o OBS em primeiro plano).
4. Para tirar o aviso antes do tempo: **Limpar aviso da TV**.

### Atalhos padrão

| Atalho         | O que faz                              |
| -------------- | -------------------------------------- |
| `Ctrl+Alt+1`   | "O áudio não está chegando"            |
| `Ctrl+Alt+2`   | "Favor colocar o fone/headset"         |
| `Ctrl+Alt+0`   | Limpar o aviso da TV                   |

Se algum atalho já estiver em uso por outro programa, o app avisa na parte de
baixo da janela.

## Mudando mensagens e atalhos

Clique em **Editar mensagens e atalhos…** (abre o Bloco de Notas), altere e salve,
depois clique em **Recarregar**. Exemplo de `configuracoes.json`:

```json
{
  "duracaoDoAvisoEmSegundos": 30,
  "telaDaTv": "\\\\.\\DISPLAY2",
  "atalhoParaLimpar": "Ctrl+Alt+0",
  "mensagensRapidas": [
    { "texto": "O áudio não está chegando", "atalho": "Ctrl+Alt+1" },
    { "texto": "Favor colocar o fone/headset", "atalho": "Ctrl+Alt+2" },
    { "texto": "Abaixar o volume do louvor", "atalho": "Ctrl+Alt+3" },
    { "texto": "Microfone do pastor sem som" }
  ]
}
```

- `atalho` é opcional. Exemplos válidos: `Ctrl+Alt+3`, `Ctrl+Shift+A`, `F9`, `Alt+F2`.
- `telaDaTv` é preenchido sozinho quando você escolhe a tela no app.
- A duração e a tela também podem ser mudadas direto na janela do app.

## Para quem vai mexer no código

Feito em **C# + WPF (.NET 10)**. Estrutura:

```
src/IevcLivestreamHelper/
├── App.xaml(.cs)            Início do app: carrega a configuração e abre as janelas
├── Views/
│   ├── ControlWindow        Janela do operador (botões, mensagem, configurações)
│   └── OverlayWindow        O aviso na TV (visual e animações)
├── Alerts/AlertService.cs   Qual aviso está na TV e quanto tempo falta
├── Hotkeys/                 Atalhos globais de teclado
├── Monitors/                Lista de monitores e escolha da TV
├── Settings/                Leitura e gravação do configuracoes.json
├── Native/NativeMethods.cs  Chamadas à API do Windows
└── Assets/sininho.ico       Ícone
```

### Compilar

Precisa do [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# Rodar (no Windows)
dotnet run --project src/IevcLivestreamHelper

# Gerar o .exe portátil em ./publish
dotnet publish src/IevcLivestreamHelper -c Release -o publish
```

### Publicação automática

A cada push na `main`, o GitHub Actions
([`.github/workflows/publicar.yml`](.github/workflows/publicar.yml)) gera o
`SininhoDaLive.exe` e cria uma Release nova (`v1.0.<número>`). Em pull requests
ele só compila, para conferir que está tudo certo.
