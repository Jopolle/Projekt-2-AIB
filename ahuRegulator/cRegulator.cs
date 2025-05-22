using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ahuKlasy;

namespace ahuRegulator
{

    #region ParametryLokalne

    // przykładowa realizacja - do modyfikacji przez studenta
    class cRegulatorPI
    {
        double Ts = 1;  
        
        //Watunki poczatkowe calki 
        public double calka = 0;
        
        //Wzmocnienia regulatora
        public double kp = 1;
        public double ki = 0;

        //Ograniczenia wyjscia - np. w procentach
        public double u_max = 100;
        public double u_min = -100;


        public double Wyjscie(double Uchyb)
        {
           // Wartosc calki to calka z podzedniej chwili + uchyb * czas trwania tego uchybu
            calka = calka + Uchyb * Ts;
           
            //Obliczenie sygnalu sterujacego regulatora i przeliczenie zeby bylo w minutach
            double u = kp * Uchyb + ki * calka/60;

            //Antiwindup
            if (u > u_max)
            {
                calka = ((u_max - kp * Uchyb) * 60) / ki;
                u = u_max;
            }
            else if (u < u_min)
            {
                calka = ((u_min - kp * Uchyb) * 60) / ki;
                u = u_min;
            }
            
            //Wyjscie sygnalu sterujacego
            return u;

        }

    }

    #endregion

    /// <summary>
    /// przykładowe stany pracy centrali - do zmiany podkądem właściwego projektu
    /// </summary>
    public enum eStanyPracyCentrali
    {
        Stop = 0,
        Praca = 1,
        RozruchWentylatora = 2,
        WychladzanieNagrzewnicy = 3,
        AlarmNagrzewnicy = 4,
        AlarmWymiennika = 5

    }



    public class cRegulator
    {

        // ******** tych zmiennych nie ruszamy - są wykorzystywane przez program wywołujący
        public cDaneWeWy DaneWejsciowe = null;
        public cDaneWeWy DaneWyjsciowe = null;
        public double Ts = 1; //czas, co jaki jest wywoływana procedura regulatora


        // ********* zmienne definiowane przez studenta

        cRegulatorPI RegPI = new cRegulatorPI();


        bool wymagany_reset = false;                        //zmienna wymuszajaca wlaczenie pracy jeszcze raz przez uzytkownika

        //Procedura przeciwzamrozeniowa nagrzewnicy wodnej
        double czas_od_zaniku_nagrz = 0;
        bool procedura_nagrz_trwa = false;

        //Procedura przeciwzamrozeniowa wymmienika
        double czas_od_zaniku_wym = 0;
        bool procedura_wym_trwa = false;



        eStanyPracyCentrali StanPracyCentrali = eStanyPracyCentrali.Stop;


        double CzasOdStartu = 0;
        double CzasOdStopu = 0;
        double OpoznienieZalaczeniaNagrzewnicy_s = 10;
        double OpoznienieWylaczeniaWentylatora_s = 15;

        // ***************************************************

        /// <summary>
        /// funkcja wywoływana przez zewnętrzny program co czas Ts
        /// </summary>
        /// <returns></returns>
        public int iWywolanie()
        {
            // wnętrze funkcji dowolnie zmieniane przez studenta


            // Odczyt danych wejsciowych
            double t_zad = DaneWejsciowe.Czytaj(eZmienne.TempZadana_C);                                                     //temperatura zadana
            double t_pom = DaneWejsciowe.Czytaj(eZmienne.TempPomieszczenia_C);                                              //temperatura pomieszczenia
            double t_cz = DaneWejsciowe.Czytaj(eZmienne.TempCzerpni_C);                                                     //temperatura czerpni
            double t_wyrz = DaneWejsciowe.Czytaj(eZmienne.TempWyrzutni_C);                                                  //temperatura wyrzutni
            double t_naw = DaneWejsciowe.Czytaj(eZmienne.TempNawiewu_C);                                                    //temperatura nawiewu
            double t_wyw = DaneWejsciowe.Czytaj(eZmienne.TempWywiewu_C);                                                    //temperatura wywiewu

            bool boStart = DaneWejsciowe.Czytaj(eZmienne.PracaCentrali) > 0;                                                //sygnal startu
            bool procedura_nagrzewnica = DaneWejsciowe.Czytaj(eZmienne.TermostatPZamrNagrzewnicyWodnej) > 0;                //termostat nagrzewnicy wodnej
            bool procedura_wymiennik = DaneWejsciowe.Czytaj(eZmienne.TempZaOdzyskiem_C) < 5;                                //zabezpieczenie wymmienika krzyzowego

            // algorytm sterowania
            double y_nagrz = 0;
            bool boPracaWentylatoraNawiewu = false;



            if (procedura_nagrzewnica && StanPracyCentrali != eStanyPracyCentrali.AlarmNagrzewnicy)
            {
                StanPracyCentrali = eStanyPracyCentrali.AlarmNagrzewnicy;
                czas_od_zaniku_nagrz = 0;
            }
            else if (procedura_wymiennik
                     && StanPracyCentrali != eStanyPracyCentrali.AlarmNagrzewnicy && StanPracyCentrali != eStanyPracyCentrali.AlarmWymiennika)
            {
                StanPracyCentrali = eStanyPracyCentrali.AlarmWymiennika;
                czas_od_zaniku_wym = 0;
            }





            switch (StanPracyCentrali)
                {
                    case eStanyPracyCentrali.Stop:
                        {
                            y_nagrz = 0;
                            DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 0);
                            DaneWyjsciowe.Zapisz(eZmienne.Wysterowanie_bypass_pr, 0);
                            boPracaWentylatoraNawiewu = false;
                            if (!boStart)
                            {
                                wymagany_reset = false;
                            }

                            if (boStart & !wymagany_reset)
                            {
                                CzasOdStartu = 0;
                                StanPracyCentrali = eStanyPracyCentrali.RozruchWentylatora;

                            }
                            break;
                        }
                    case eStanyPracyCentrali.RozruchWentylatora:
                        {
                            boPracaWentylatoraNawiewu = true;

                            if (CzasOdStartu < OpoznienieZalaczeniaNagrzewnicy_s)
                            {
                                y_nagrz = 0;
                                CzasOdStartu += Ts;
                            }
                            else
                            {
                                StanPracyCentrali = eStanyPracyCentrali.Praca;
                                y_nagrz = RegPI.Wyjscie(t_zad - t_pom);
                            }


                            break;
                        }
                    case eStanyPracyCentrali.Praca:
                        {
                            boPracaWentylatoraNawiewu = true;

                            if (!boStart)
                            {
                                y_nagrz = 0;
                                StanPracyCentrali = eStanyPracyCentrali.WychladzanieNagrzewnicy;
                            }
                            else
                            {
                                y_nagrz = RegPI.Wyjscie(t_zad - t_pom);
                            }

                            break;
                        }
                    case eStanyPracyCentrali.WychladzanieNagrzewnicy:
                        {
                            if (CzasOdStopu < OpoznienieWylaczeniaWentylatora_s)
                            {
                                CzasOdStopu += Ts;
                                y_nagrz = 0;
                                boPracaWentylatoraNawiewu = true;
                            }
                            else
                            {
                                StanPracyCentrali = eStanyPracyCentrali.Stop;
                                y_nagrz = 0;
                                boPracaWentylatoraNawiewu = true;
                            }

                            break;
                        }

                    case eStanyPracyCentrali.AlarmNagrzewnicy:
                        {

                            wymagany_reset = true;

                        //Dodatkowe sprawdzenie czy jednoczesnie nie wlaczyla sie ochrona wymiennika bo to ze soba nie koliduje
                        if (procedura_wymiennik)
                        {
                            
                            DaneWyjsciowe.Zapisz(eZmienne.Wysterowanie_bypass_pr, 100);
                        }
                        else
                        {
                            DaneWyjsciowe.Zapisz(eZmienne.Wysterowanie_bypass_pr, 0);
                        }

                        if (procedura_nagrzewnica)
                            {
                                y_nagrz = 100;
                                boPracaWentylatoraNawiewu = false;
                                DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 1);
                                czas_od_zaniku_nagrz = 0;
                                procedura_nagrz_trwa = true;
                            }
                            else
                            {

                                if (procedura_nagrz_trwa)
                                {
                                    czas_od_zaniku_nagrz += Ts;

                                    if (czas_od_zaniku_nagrz > 5)
                                    {
                                        procedura_nagrz_trwa = false;
                                        StanPracyCentrali = eStanyPracyCentrali.Stop;

                                    }

                                }

                                y_nagrz = 100;
                                boPracaWentylatoraNawiewu = false;
                                DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 1);

                            }

                            break;
                        }
                    case eStanyPracyCentrali.AlarmWymiennika:
                        {

                        wymagany_reset = true;

                        if (procedura_wymiennik)
                        {
                            
                            DaneWyjsciowe.Zapisz(eZmienne.Wysterowanie_bypass_pr, 100);
                            DaneWyjsciowe.Zapisz(eZmienne.WysterowanieNagrzewnicy1_pr, 0);
                            DaneWyjsciowe.Zapisz(eZmienne.WysterowanieChlodnicy_pr, 0);
                            DaneWyjsciowe.Zapisz(eZmienne.ZezwolenieNaPraceWentylatoraNawiewu, 0);
                            DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 0);

                            czas_od_zaniku_wym = 0;
                            procedura_wym_trwa = true;
                        }
                        else
                        {
                            
                            if (procedura_wym_trwa)
                            {
                                czas_od_zaniku_wym += Ts;

                                if (czas_od_zaniku_wym > 5)
                                {
                                    procedura_wym_trwa = false;
                                    StanPracyCentrali = eStanyPracyCentrali.Stop;
                                }
                            }

                            
                            DaneWyjsciowe.Zapisz(eZmienne.Wysterowanie_bypass_pr, 100);
                            DaneWyjsciowe.Zapisz(eZmienne.WysterowanieNagrzewnicy1_pr, 0);
                            DaneWyjsciowe.Zapisz(eZmienne.WysterowanieChlodnicy_pr, 0);
                            DaneWyjsciowe.Zapisz(eZmienne.ZezwolenieNaPraceWentylatoraNawiewu, 0);
                            DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 0);
                        }

                        break;
                    }


                }



            // ustawienie wyjść
            DaneWyjsciowe.Zapisz(eZmienne.WysterowanieNagrzewnicy1_pr, y_nagrz);
            DaneWyjsciowe.Zapisz(eZmienne.ZezwolenieNaPraceWentylatoraNawiewu, boPracaWentylatoraNawiewu);



            return 0;
        }







        /// <summary>
        /// wywołanie formularza z parametrami
        /// </summary>
        public void ZmienParametry()
        {
            // wnętrze funkcji dowolnie zmieniane przez studenta
            fmParametry fm = new fmParametry();
            fm.kp = RegPI.kp;
            fm.ki = RegPI.ki;

            fm.t1 = OpoznienieZalaczeniaNagrzewnicy_s;
            fm.t2 = OpoznienieWylaczeniaWentylatora_s;


            if (fm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RegPI.kp = fm.kp;
                RegPI.ki = fm.ki;

                OpoznienieZalaczeniaNagrzewnicy_s = fm.t1;
                OpoznienieWylaczeniaWentylatora_s = fm.t2;

            }
        }

    }
}
