# Avisos para a TV

Helper do [IEVC Livestream Helper](../README.md).

O operador da **mesa de corte** (OBS) manda um aviso, e ele aparece **bem grande
na TV da mesa de som**, que fica em outra sala. O aviso aparece na
**parte de baixo** da TV.

- Só visual (sem som), fofinho mas impossível de não ver: cartão arredondado
  amarelo/coral, sino balançando, pulsação suave e uma barrinha mostrando quanto
  tempo falta.
- O aviso some sozinho depois de **15 segundos** (dá para mudar) ou quando o
  operador clica em **Limpar**.
- Aparece **por cima do projetor do OBS**, não recebe cliques, não rouba o foco e
  **não vai para a transmissão**.

## Como usar

1. Abra o **IEVC Livestream Helper**. Aparece uma **janelinha flutuante** no
   canto da tela, sempre por cima das outras janelas (inclusive do OBS).
2. Clique na **engrenagem** ⚙, escolha em **Tela da TV** o monitor que é a TV da
   mesa de som e clique em **Testar** para conferir.
3. Para mandar um aviso, clique num dos botões:
   - **O áudio não está chegando**
   - **Favor colocar o fone/headset**

   ou escreva outra mensagem na caixa **Outra mensagem** e clique em
   **Enviar para a TV** (ou aperte **Enter**; **Shift+Enter** pula linha).
4. Para tirar o aviso antes do tempo: **Limpar aviso da TV**.

### A janelinha

- **Mudar de lugar:** segure o título da janelinha e arraste. Ela lembra onde
  ficou da próxima vez que abrir.
- **Recolher:** clique no **–**. Ela vira uma **bolinha** com o ícone do app.
  Enquanto há aviso na TV, a bolinha mostra quantos segundos faltam.
  Clique na bolinha para abrir de novo, ou segure e arraste para mudar de lugar.
- **Fechar:** clique no **×** (o app pergunta antes, porque o aviso da TV some).

## Mudando as mensagens

Clique na engrenagem ⚙ → **Editar mensagens…** (abre o Bloco de Notas), altere e
salve, depois clique em **Recarregar**. Exemplo de `configuracoes.json`:

```json
{
  "duracaoDoAvisoEmSegundos": 15,
  "mensagensRapidas": [
    { "texto": "O áudio não está chegando" },
    { "texto": "Favor colocar o fone/headset" }
  ]
}
```

- Para ter mais um botão, acrescente uma linha `{ "texto": "..." }` na lista
  (lembre da vírgula entre as linhas).
- A duração e a tela da TV também podem ser mudadas direto na janelinha.
- O arquivo também guarda a tela da TV (`telaDaTv`) e a posição da janelinha
  (`posicaoDaJanela`); não precisa mexer nelas.
