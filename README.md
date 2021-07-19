# Ruby_tBot

Implement in telegram bot that connects to Binance and Kraken
exchanges using ExchangeSharp library and display trades and candles
in real time

The telegram bot will have a folowing functionality:

button - select exchange
button - select a market symbol
button - select data(trade, candle)

Next telegram bot will subscribe to websocket (trades, candel) of a corresponding
exchange with a corresponding symbol

After whitc, a message will be send to user, that will be update every time
a new update come from the WebSocket

That is, let's say the following message will be return

Candle: USDT-BTC Binance
Price: 40.000 Base Volume: 10000000 Quote Volume: 500

Let's say in a second an update of the candle WebSocket came. Then the 
existing message will be update vs. a new one sent.
