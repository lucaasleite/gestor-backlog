import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styles: ':host { display: contents; }',
})
export class App {
  // Injetado só pra garantir que o tema salvo seja aplicado assim que o app sobe.
  constructor(private readonly theme: ThemeService) {}
}
