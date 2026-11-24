# Avisos para a TV

Helper do [IEVC Livestream Helper](../README.md).

O operador da **mesa de corte** (OBS) manda um aviso, e ele aparece **bem grande
na TV da mesa de som**, que fica em outra sala.

- Só visual (sem som), fofinho mas impossível de não ver: cartão arredondado
  amarelo/coral, sino balançando, pulsação suave e uma barrinha mostrando quanto
  tempo falta.
- O aviso some sozinho depois de **30 segundos** (dá para mudar) ou quando o
  operador clica em **Limpar**.
- Aparece **por cima do projetor do OBS**, não recebe cliques, não rouba o foco e
  **não vai para a transmissão**.

## Como usar

1. Abra o **IevcLivestreamHelper.exe** no computador do OBS.
2. Em **Tela da TV**, escolha o monitor que é a TV da mesa de som e clique em
   **Testar** para conferir.
3. Para mandar um aviso:
   - clique em uma **mensagem rápida**, ou
   - digite uma **mensagem personalizada** e aperte **Enter**, ou
   - use os **atalhos de teclado** (funcionam mesmo com o OBS em primeiro plano).
4. Para tirar o aviso antes do tempo: **Limpar aviso da TV**.

### Atalhos padrão

| Atalho       | O que faz                      |
| ------------ | ------------------------------ |
| `Ctrl+Alt+1` | "O áudio não está chegando"    |
| `Ctrl+Alt+2` | "Favor colocar o fone/headset" |
| `Ctrl+Alt+0` | Limpar o aviso da TV           |

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
