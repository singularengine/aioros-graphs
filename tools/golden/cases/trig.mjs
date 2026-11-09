export const d3Version = null;

function lcg(seed) {
  let s = seed;
  return () => (s = (1664525 * s + 1013904223) % 4294967296) / 4294967296;
}

export default function() {
  const angle = Math.PI * (3 - Math.sqrt(5));
  const values = [0, -0, Infinity, -Infinity, NaN, Math.PI / 4, -Math.PI / 4, 0.7853981633974483, 2.356194490192345, 1e-9, 2 ** -28, 2 ** -27];
  for (let i = 0; i < 2000; i++) values.push(i * angle);
  const random = lcg(12345);
  for (let i = 0; i < 2000; i++) values.push((random() - 0.5) * 20);
  for (let i = 0; i < 2000; i++) values.push((random() * 2 - 1) * 10 ** (random() * 310 - 10));
  for (let k = -100; k <= 100; k++) {
    const x = k * Math.PI / 2;
    values.push(x, x * (1 + 2 ** -52), x * (1 - 2 ** -52));
  }
  return {trig: values.map((x) => ({x, sin: Math.sin(x), cos: Math.cos(x)}))};
}
