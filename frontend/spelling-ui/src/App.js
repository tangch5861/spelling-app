import React, { useState } from 'react';
import './App.css';

function App() {
  const steps = ['upload', 'review', 'story', 'listen', 'speak', 'write', 'repeat'];
  const [step, setStep] = useState(steps[0]);

  const next = () => {
    const idx = steps.indexOf(step);
    setStep(steps[(idx + 1) % steps.length]);
  };

  return (
    <div className="App">
      <h1>Step: {step}</h1>
      <button onClick={next}>Next</button>
    </div>
  );
}

export default App;
