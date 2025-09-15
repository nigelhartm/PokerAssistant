const express = require('express')
const { calculateEquity } = require('poker-odds')

const app = express()
const port = 3000

// allow JSON request bodies
app.use(express.json())

// map hand types to numeric ranks
const ranks = {
  "high card": 1,
  "one pair": 2,
  "two pair": 3,
  "three of a kind": 4,
  "straight": 5,
  "flush": 6,
  "full house": 7,
  "four of a kind": 8,
  "straight flush": 9,
  "royal flush": 10
}

// retry wrapper for calculateEquity
function safeCalculateEquity(hands, board, iterations, exhaustive, retries = 5) {
  let lastError
  for (let i = 0; i < retries; i++) {
    try {
      return calculateEquity(hands, board, iterations, exhaustive)
    } catch (err) {
      lastError = err
      console.warn(`calculateEquity failed (attempt ${i + 1}/${retries}): ${err.message}`)
    }
  }
  throw lastError
}

// helper to calculate strength of a single hand
function calculateHandStrength(hand, board = [], iterations = 100000) {
  const result = safeCalculateEquity([hand], board, iterations, false)[0]
  const total = result.count

  // expected score based on probabilities of outcomes
  let score = result.handChances.reduce((sum, h) => {
    return sum + ranks[h.name] * (h.count / total)
  }, 0)

  return {
    hand: result.hand,
    simulations: total,
    distribution: result.handChances.map(h => ({
      name: h.name,
      probability: h.count / total
    })),
    strengthScore: score, // between 1 and 10
    normalizedStrength: (score - 1) / (10 - 1),
  }
}

// API route: evaluate one hand
app.post('/eval_hand', (req, res) => {
  const { hand, board } = req.body

  if (!hand || !Array.isArray(hand) || hand.length !== 2) {
    return res.status(400).json({ error: "Request must include 'hand' as an array of 2 cards" })
  }

  try {
    const result = calculateHandStrength(hand, board || [])
    console.log("Succesful Request Hand Eval")
    res.json(result)
  } catch (err) {
    console.error("Final failure after retries:", err.message)
    res.status(500).json({ error: "Failed to calculate equity after retries" })
  }
})

app.listen(port, () => {
  console.log(`Poker hand evaluator running at http://localhost:${port}`)
})
