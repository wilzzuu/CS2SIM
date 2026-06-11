def calc_btc(btc_miner_count):
    btc_rate = 1/(1800000/(1+(btc_miner_count-1)*0.041225)/3600)
    btc_time = 1800000/(1+(btc_miner_count-1)*0.041225)/3600
    btc_time_f = time_format(btc_time)
    btc_price = 100000
    money_per_hour = int(btc_rate * btc_price)

    print(f"\nCalculating BTC Mining with {btc_miner_count} miners:")
    print(f"BTC Rate = {btc_rate}/hour")
    print(f"1 BTC = {btc_time_f}")
    print(f"€/H = {money_per_hour}")

def calc_eth(eth_miner_count):
    eth_rate = 1/(1000000/(1+(eth_miner_count-1)*0.041225)/3600)
    eth_time = 1000000/(1+(eth_miner_count-1)*0.041225)/3600
    eth_time_f = time_format(eth_time)
    eth_price = 5000
    money_per_hour = int(eth_rate * eth_price)

    print(f"\nCalculating ETH Mining with {eth_miner_count} miners:")
    print(f"ETH Rate = {eth_rate}/hour")
    print(f"1 ETH = {eth_time_f}")
    print(f"€/H = {money_per_hour}")

def time_format(time):
    return '{0:02.0f}H:{1:02.0f}M'.format(*divmod(time * 60,60))

if __name__ == "__main__":
    miner_count = int(input("MINER COUNT: "))
    calc_btc(miner_count)
    calc_eth(miner_count)