import { test, expect } from '@playwright/test';
const viewports=[{width:375,height:812},{width:768,height:1024},{width:1440,height:900}];
for(const viewport of viewports) test.describe(`Experience ${viewport.width}x${viewport.height}`,()=>{
 test.use({viewport});
 test('authenticated shell exposes hierarchy without covering content',async({page})=>{test.skip(!process.env.ORCA_E2E_BASE_URL,'Requires seeded authenticated environment');await page.goto(`${process.env.ORCA_E2E_BASE_URL}/Dashboard`);await expect(page.getByRole('heading',{name:/O que vamos preparar hoje/i})).toBeVisible();await expect(page.getByRole('navigation',{name:/Navegação principal/i})).toBeVisible();});
 test('plan page explains preservation and available plan',async({page})=>{test.skip(!process.env.ORCA_E2E_BASE_URL,'Requires seeded authenticated environment');await page.goto(`${process.env.ORCA_E2E_BASE_URL}/Subscription`);await expect(page.getByText('Planos controlam acesso e capacidade')).toBeVisible();await expect(page.getByText('Plano disponível agora')).toBeVisible();});
 test('discover labels demonstration data',async({page})=>{test.skip(!process.env.ORCA_E2E_BASE_URL,'Requires seeded authenticated environment');await page.goto(`${process.env.ORCA_E2E_BASE_URL}/Discover`);await expect(page.getByText('Demonstração').first()).toBeVisible();});
});
