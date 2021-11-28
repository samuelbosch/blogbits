import random

def orchard(fruitbox_strategy):
    raven = 0
    fruits = 4 * [10]
    while sum(fruits) > 0 and raven < 9:
        n = random.randint(0,5)
        if n == 5:
            raven = raven+1
        elif n == 4:
            fruitbox_strategy(fruits)
            fruitbox_strategy(fruits)
        elif fruits[n] > 0:
            fruits[n] = fruits[n]-1
    return raven < 9

def decrease_max(fruits):
    mi, mv = 0,0
    for i, v in enumerate(fruits):
        if v > mv:
            mi,mv = i,v
    fruits[mi] = mv-1

def decrease_min(fruits):
    mi, mv = 0, 11
    for i, v in enumerate(fruits):
        if v < mv and v > 0:
            mi,mv = i,v
    if mv < 11:
        fruits[mi] = mv-1

def decrease_random(fruits):
    mi = random.randint(0,3)
    v = fruits[mi]
    if v > 0:
        fruits[mi] = v-1
    elif sum(fruits) > 0:
        decrease_random(fruits)
 
## http://en.wikipedia.org/wiki/Monte_Carlo_method 
def monte_carlo_simulation(game, count):
    won = 0
    for _ in range(count):
        if game():
            won = won + 1
    return (won*100) /count

def simulate_orchard_best(count):
    return monte_carlo_simulation(lambda:orchard(decrease_max), count)

def simulate_orchard_worst(count):
    return monte_carlo_simulation(lambda:orchard(decrease_min), count)

def simulate_orchard_random(count):
    return monte_carlo_simulation(lambda:orchard(decrease_random), count)


print('Winning rates of 10 runs of the best strategy with 50 games: \n%s' %
      ([str(simulate_orchard_best(50)) + '%' for _ in range(10)]))

print('Winning rates of 10 runs of the best strategy with 1000 games: \n%s' %
      ([str(simulate_orchard_best(1000)) + '%' for _ in range(10)]))

print('Winning rates of 10 runs of the worst strategy with 50 games: \n%s' %
      ([str(simulate_orchard_worst(50)) + '%' for _ in range(10)]))

print('Winning rates of 10 runs of the worst strategy with 1000 games: \n%s' %
      [str(simulate_orchard_worst(1000)) + '%' for _ in range(10)])

print('Winning rates of 10 runs of the random strategy with 50 games: \n%s' % 
      ([str(simulate_orchard_random(50)) + '%' for _ in range(10)]))

print('Winning rates of 10 runs of the random strategy with 1000 games: \n%s' %
      [str(simulate_orchard_random(1000)) + '%' for _ in range(10)]) 
