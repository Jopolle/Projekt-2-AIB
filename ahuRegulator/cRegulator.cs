using System;
using System.Collections.Generic;
using System.Data;
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
        public double kp = 2;
        public double ki = 0;

        //Ograniczenia wyjscia - np. w procentach
        public double u_max = 100;
        public double u_min = -100;


        public double Wyjscie(double Uchyb)
        {
            // Wartosc calki to calka z podzedniej chwili + uchyb * czas trwania tego uchybu
            calka = calka + Uchyb * Ts;

            //Obliczenie sygnalu sterujacego regulatora i przeliczenie zeby bylo w minutach
            double u = kp * Uchyb + ki * calka / 60;

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

        AlarmWymiennika = 5,



        //te będą zagnieżdżone
        Praca_odzyskciepla = 6,
        Praca_odzyskchlodu = 7,
        Praca_grzanie = 8,
        Praca_chlodzenie = 9,
        Praca_jalowa = 10,
        ProceduraPrzeciwzamrozeniowa = 11


    }



    public class cRegulator
    {

        // ******** tych zmiennych nie ruszamy - są wykorzystywane przez program wywołujący
        public cDaneWeWy DaneWejsciowe = null;
        public cDaneWeWy DaneWyjsciowe = null;
        public double Ts = 1; //czas, co jaki jest wywoływana procedura regulatora


        // ********* zmienne definiowane przez studenta

        cRegulatorPI RegPI = new cRegulatorPI();
        cRegulatorPI RegPI2 = new cRegulatorPI();





        bool wymagany_reset = false;

        //Procedura przeciwzamrozeniowa nagrzewnicy wodnej
        double czas_od_zaniku_nagrz = 0;
        bool procedura_nagrz_trwa = false;



        eStanyPracyCentrali StanPracyCentrali = eStanyPracyCentrali.Stop;
        eStanyPracyCentrali TypPracyCentrali = eStanyPracyCentrali.Stop;

        double CzasOdStartu = 0;
        double CzasOdStopu = 0;
        double OpoznienieZalaczeniaNagrzewnicy_s = 10;
        double OpoznienieWylaczeniaWentylatora_s = 15;

        double TempChlodzenia = 30;
        double TempGrzania = 15;



        double DeadbandTemp = 0.5; // Martwa strefa dla temperatury pomieszczenia (w stopniach Celsjusza)
        double TempOdzyskCieplaThreshold = 10; // Temperatura czerpni poniżej której rozważamy odzysk ciepła
        double TempOdzyskChloduThreshold = 20;
        double RegulatorOutputThreshold = 5;


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



            RegPI2.kp = 2;
            //RegPI2.ki = 1;




            bool boStart = DaneWejsciowe.Czytaj(eZmienne.PracaCentrali) > 0;                                                //sygnal startu
            bool procedura_nagrzewnica = DaneWejsciowe.Czytaj(eZmienne.TermostatPZamrNagrzewnicyWodnej) > 0;                //termostat nagrzewnicy wodnej


            // algorytm sterowania
            double y_nagrz = 0;
            double y_bypass = 0;
            double y_chlodnica = 0;

            bool boPracaWentylatoraNawiewu = false;
            bool boPracaWentylatoraWywiewu = false;

            double t_naw_zad = 20;

            if (procedura_nagrzewnica && StanPracyCentrali != eStanyPracyCentrali.AlarmNagrzewnicy) //zabezpieczenie zeby nie skakać miedzy dwoma stanami non stop
            {
                StanPracyCentrali = eStanyPracyCentrali.AlarmNagrzewnicy;
                czas_od_zaniku_nagrz = 0;
            }



            switch (StanPracyCentrali)
            {
                case eStanyPracyCentrali.Stop:
                    {
                        y_nagrz = 0;
                        DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 0);
                        boPracaWentylatoraNawiewu = false;
                        boPracaWentylatoraWywiewu = false;
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
                        //boPracaWentylatoraNawiewu = true;
                        // boPracaWentylatoraWywiewu = true;

                        if (CzasOdStartu < OpoznienieZalaczeniaNagrzewnicy_s)
                        {
                            y_nagrz = 0;
                            CzasOdStartu += Ts;
                        }
                        else
                        {
                            StanPracyCentrali = eStanyPracyCentrali.Praca;
                            // y_nagrz = RegPI.Wyjscie(t_zad - t_pom);
                        }


                        break;
                    }
                case eStanyPracyCentrali.Praca:
                    {
                        boPracaWentylatoraNawiewu = true;
                        boPracaWentylatoraWywiewu = true;


                        if (!boStart)
                        {
                            y_nagrz = 0;
                            y_chlodnica = 0;
                            y_bypass = 0;
                            DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 0);
                            StanPracyCentrali = eStanyPracyCentrali.WychladzanieNagrzewnicy;
                        }
                        else
                        {

                            t_naw_zad = Math.Max(10, Math.Min(40, RegPI.Wyjscie(t_zad - t_pom) + 30));



                            double regPI2_raw_output = RegPI2.Wyjscie(t_naw_zad - t_naw);






                            if (regPI2_raw_output > 0 && regPI2_raw_output <= 30)// &&
                                                                                 // t_zad > TempOdzyskCieplaThreshold && t_cz > TempOdzyskCieplaThreshold)
                            {
                                TypPracyCentrali = eStanyPracyCentrali.Praca_odzyskciepla;
                            }
                            // 2. Priorytet dla silnego grzania
                            // Zakres: poniżej -30 (np. [-100, -30)) dla regPI2_raw_output
                            else if (regPI2_raw_output > 30)
                            {
                                TypPracyCentrali = eStanyPracyCentrali.Praca_grzanie;
                            }
                            // 3. Priorytet dla silnego chłodzenia
                            // Zakres: powyżej 30 (np. (30, 100]) dla regPI2_raw_output
                            else if (regPI2_raw_output < -30)
                            {
                                TypPracyCentrali = eStanyPracyCentrali.Praca_chlodzenie;
                            }
                            // 4. Priorytet dla odzysku chłodu
                            // Zakres: (0, 30] dla regPI2_raw_output
                            else if (regPI2_raw_output < 0 && regPI2_raw_output >= -30)
                            {
                                TypPracyCentrali = eStanyPracyCentrali.Praca_odzyskchlodu;
                            }
                            // 5. Stan jałowy
                            // Gdy regPI2_raw_output wynosi 0 lub nie mieści się w żadnym z powyższych zakresów
                            else
                            {
                                TypPracyCentrali = eStanyPracyCentrali.Praca_jalowa;
                            }




                            System.Diagnostics.Debug.WriteLine("wyjscie reg2: " + regPI2_raw_output.ToString() + "    Wyjscie reg 1: " + t_naw_zad + "    Typ Pracy: " + TypPracyCentrali);

                            // Pozostała część switcha TypPracyCentrali pozostaje bez zmian,
                            // ponieważ już wykorzystuje t_naw_zad i t_naw do sterowania kaskadowego.
                            // Ważne: Wewnątrz tych case'ów, RegPI2.Wyjscie() będzie wywoływane ponownie z aktualnym uchybem,
                            // co jest poprawne dla ciągłej regulacji w danym trybie pracy.
                            switch (TypPracyCentrali)
                            {
                                case eStanyPracyCentrali.Praca_jalowa:
                                    {
                                        // Praca jałowa: brak grzania/chłodzenia, pełne otwarcie bypassu
                                        y_nagrz = 0;
                                        y_chlodnica = 0;
                                        y_bypass = 100; // Pełne otwarcie bypassu (0-100%)
                                        DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 0); // Pompa wyłączona
                                                                                                             // RegPI.Reset(); // RegPI nie resetujemy, bo jest nadrzędny i ciągle pracuje
                                                                                                             // RegPI2.Reset(); // RegPI2 jest resetowany powyżej, jeśli przechodzimy na jałową
                                        break;
                                    }
                                case eStanyPracyCentrali.Praca_grzanie:
                                    {
                                        y_nagrz = Mapuj(regPI2_raw_output, 30, 100, 0, 100);
                                        y_chlodnica = 0; // Chłodnica wyłączona
                                        y_bypass = 0;    // Bypass zamknięty
                                        DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 1); // Pompa włączona
                                        break;
                                    }
                                case eStanyPracyCentrali.Praca_chlodzenie:
                                    {
                                        y_chlodnica = Mapuj(regPI2_raw_output, -100, -30, 0, 100);
                                        y_nagrz = 0; // Nagrzewnica wyłączona
                                        y_bypass = 0;    // Bypass zamknięty
                                        DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 0); // Pompa wyłączona
                                        break;
                                    }
                                case eStanyPracyCentrali.Praca_odzyskciepla:
                                    {

                                        y_bypass = Mapuj(regPI2_raw_output, 0, 30, 0, 100);
                                        y_nagrz = 0;     // Nagrzewnica wyłączona
                                        y_chlodnica = 0; // Chłodnica wyłączona
                                        DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 0); // Pompa wyłączona
                                        break;
                                    }
                                case eStanyPracyCentrali.Praca_odzyskchlodu:
                                    {
                                        y_bypass = Mapuj(regPI2_raw_output, -30, 0, 0, 100);
                                        y_nagrz = 0;     // Nagrzewnica wyłączona
                                        y_chlodnica = 0; // Chłodnica wyłączona
                                        DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 0); // Pompa wyłączona
                                        break;
                                    }
                            }
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
                            boPracaWentylatoraWywiewu = true;
                        }
                        else
                        {
                            StanPracyCentrali = eStanyPracyCentrali.Stop;
                            y_nagrz = 0;
                            boPracaWentylatoraNawiewu = true;
                            boPracaWentylatoraWywiewu = true;
                        }

                        break;
                    }


                case eStanyPracyCentrali.AlarmNagrzewnicy:
                    {

                        wymagany_reset = true;

                        if (procedura_nagrzewnica)
                        {
                            y_nagrz = 100;
                            boPracaWentylatoraNawiewu = false;
                            boPracaWentylatoraWywiewu = false;
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
                            boPracaWentylatoraWywiewu = false;
                            DaneWyjsciowe.Zapisz(eZmienne.ZalaczeniePompyNagrzewnicyWodnej1, 1);

                        }

                        break;
                    }

                case eStanyPracyCentrali.ProceduraPrzeciwzamrozeniowa:
                    break;





            }



            // ustawienie wyjść
            DaneWyjsciowe.Zapisz(eZmienne.WysterowanieNagrzewnicy1_pr, y_nagrz);
            DaneWyjsciowe.Zapisz(eZmienne.Wysterowanie_bypass_pr, y_bypass);
            DaneWyjsciowe.Zapisz(eZmienne.WysterowanieChlodnicy_pr, y_chlodnica);

            DaneWyjsciowe.Zapisz(eZmienne.ZezwolenieNaPraceWentylatoraNawiewu, boPracaWentylatoraNawiewu);
            DaneWyjsciowe.Zapisz(eZmienne.ZezwolenieNaPraceWentylatoraWywiewu, boPracaWentylatoraWywiewu);


            return 0;
        }

        public static double Mapuj(double value, double inMin, double inMax, double outMin, double outMax)
        {
            if (Math.Abs(inMax - inMin) < double.Epsilon) return outMin;

            return (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
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
